using Firebase;
using Firebase.Analytics;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Functions;
using Firebase.Messaging;
using Firebase.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public static class FirebaseExtension
{
    #region Initialize
    public static async Task<FirebaseResult<DependencyStatus>> InitializeAsync()
    {
        FirebaseResult<DependencyStatus> result;

        try
        {
            var status = await FirebaseApp.CheckAndFixDependenciesAsync();

            result = new FirebaseResult<DependencyStatus>(status);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif

            result = new FirebaseResult<DependencyStatus>(false);
        }

        return result;
    }
    #endregion

    #region Analytics
    public static void Log(string eventName)
    {
        FirebaseAnalytics.LogEvent(eventName);
    }

    public static void Log(string eventName, string parameterName, double parameterValue)
    {
        FirebaseAnalytics.LogEvent(eventName, parameterName, parameterValue);
    }
    #endregion

    #region Auth
    public static string Uid => SignedIn ? _auth.CurrentUser.UserId : null;
    public static bool SignedIn => _auth.CurrentUser != null;

    private static readonly FirebaseAuth _auth = FirebaseAuth.DefaultInstance;

    public static async Task<FirebaseResult> DeleteUserAsync()
    {
        FirebaseResult result;

        try
        {
            await _auth.CurrentUser.DeleteAsync();

            result = new FirebaseResult(true);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif
            result = new FirebaseResult(false);
        }

        return result;
    }

    public static async Task<FirebaseResult> FacebookSignInAsync()
    {
        // get token
        string token = null; // await FacebookWrapper.SignInAsync();

        // check whether successfully signed in with facebook auth
        if (string.IsNullOrEmpty(token)) return new FirebaseResult(false);

        FirebaseResult result;

        try
        {
            // get credential
            var credential = FacebookAuthProvider.GetCredential(token);

            // sign in with facebook credential
            await _auth.SignInWithCredentialAsync(credential);

            result = new FirebaseResult(true);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif
            result = new FirebaseResult(false);
        }

        return result;
    }

    public static async Task<FirebaseResult> GoogleSignInAsync()
    {
        // get token
        string token = null; // await GoogleWrapper.SignInAsync();

        // check whether successfully signed in with google auth
        if (string.IsNullOrEmpty(token)) return new FirebaseResult(false);

        FirebaseResult result;

        try
        {
            // get credential
            var credential = GoogleAuthProvider.GetCredential(token, null);

            // sign in with google credential
            await _auth.SignInWithCredentialAsync(credential);

            result = new FirebaseResult(true);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif
            result = new FirebaseResult(false);
        }

        return result;
    }

    public static async Task<FirebaseResult> KakaoSignInAsync()
    {
        // get token
        string token = null; // await KakaoWrapper.SignInAsync();

        // check whether successfully signed in with kakao auth
        if (string.IsNullOrEmpty(token)) return new FirebaseResult(false);

        FirebaseResult result;

        try
        {
            // sign in with custom token
            await _auth.SignInWithCustomTokenAsync(token);

            result = new FirebaseResult(true);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif
            result = new FirebaseResult(false);
        }

        return result;
    }

    public static FirebaseResult SignOut()
    {
        FirebaseResult result;

        try
        {
            _auth.SignOut();

            result = new FirebaseResult(true);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif
            result = new FirebaseResult(false);
        }

        return result;
    }

    public static async Task CustomSignUpAsync(string email, string password)
    {
        try
        {
            await _auth.CreateUserWithEmailAndPasswordAsync(email, password);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif
        }
    }

    public static async Task<bool> CustomSignInAsync(string email, string password)
    {
        try
        {
            await _auth.SignInWithEmailAndPasswordAsync(email, password);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif
        }

        // return whether successfully signed in
        return SignedIn;
    }
    #endregion

    #region Database
    public enum ValueListenerType { Changed }
    public enum ChildListenerType { Added, Removed, Moved, Changed }

    private static readonly FirebaseDatabase _db = FirebaseDatabase.DefaultInstance;

    private static Dictionary<string, DatabaseReference> _refDic = new Dictionary<string, DatabaseReference>();

    public static void AddListener(ChildListenerType type, string path, EventHandler<ChildChangedEventArgs> action)
    {
        switch (type)
        {
            case ChildListenerType.Added:
                GetReference(path).ChildAdded += action;
                break;

            case ChildListenerType.Removed:
                GetReference(path).ChildRemoved += action;
                break;

            case ChildListenerType.Moved:
                GetReference(path).ChildMoved += action;
                break;

            case ChildListenerType.Changed:
                GetReference(path).ChildChanged += action;
                break;
        }
    }

    public static void AddListener(ValueListenerType type, string path, EventHandler<ValueChangedEventArgs> action)
    {
        switch (type)
        {
            case ValueListenerType.Changed:
                GetReference(path).ValueChanged += action;
                break;
        }
    }

    public static async Task<FirebaseResult<bool>> CheckAsync(string path)
    {
        FirebaseResult<bool> result;

        try
        {
            var snapshot = await GetReference(path).GetValueAsync();

            result = new FirebaseResult<bool>(true, snapshot.Exists);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif
            result = new FirebaseResult<bool>(false);
        }

        return result;
    }

    public static string GenerateInstanceId(string path)
    {
        return GetReference(path).Push().Key;
    }

    private static DatabaseReference GetReference(string path)
    {
        if (!_refDic.ContainsKey(path))
        {
            _refDic.Add(path, _db.GetReference(path));
        }

        return _refDic[path];
    }

    public static async Task<FirebaseResult<long>> GetCountAsync(string path)
    {
        FirebaseResult<long> result;

        try
        {
            var snapshot = await GetReference(path).GetValueAsync();

            result = new FirebaseResult<long>(snapshot.ChildrenCount);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif
            result = new FirebaseResult<long>(false);
        }

        return result;
    }

    public static async Task<FirebaseResult<T>> GetJsonAsync<T>(string path)
    {
        FirebaseResult<T> result;

        try
        {
            DataSnapshot ss = await GetReference(path).GetValueAsync();

            result = new FirebaseResult<T>(ss.Exists ? JsonUtility.FromJson<T>(ss.GetRawJsonValue()) : default(T));
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif
            result = new FirebaseResult<T>(false);
        }

        return result;
    }

    public static async Task<FirebaseResult<List<T>>> GetJsonsAsync<T>(string path)
    {
        FirebaseResult<List<T>> result;

        try
        {
            DataSnapshot ss = await GetReference(path).GetValueAsync();

            List<T> values = null;

            // check there are valid data
            if (ss.Exists && ss.ChildrenCount > 0)
            {
                values = new List<T>((int)ss.ChildrenCount);

                foreach (var child in ss.Children)
                {
                    values.Add(JsonUtility.FromJson<T>(child.GetRawJsonValue()));
                }
            }

            result = new FirebaseResult<List<T>>(values);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif
            result = new FirebaseResult<List<T>>(false);
        }

        return result;
    }

    public static async Task<FirebaseResult<List<string>>> GetKeysAsync(string path)
    {
        FirebaseResult<List<string>> result;

        try
        {
            DataSnapshot ss = await GetReference(path).GetValueAsync();

            List<string> keys = null;

            // check there are valid data
            if (ss.Exists && ss.ChildrenCount > 0)
            {
                keys = new List<string>((int)ss.ChildrenCount);

                foreach (var child in ss.Children)
                {
                    keys.Add(child.Key);
                }
            }

            result = new FirebaseResult<List<string>>(keys);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif
            result = new FirebaseResult<List<string>>(false);
        }

        return result;
    }

    public static async Task<FirebaseResult<Dictionary<string, T>>> GetKeyJsonsAsync<T>(string path)
    {
        FirebaseResult<Dictionary<string, T>> result;

        try
        {
            DataSnapshot ss = await GetReference(path).GetValueAsync();

            Dictionary<string, T> dic = null;

            // check there are valid data
            if (ss.Exists && ss.ChildrenCount > 0)
            {
                dic = new Dictionary<string, T>();

                foreach (var child in ss.Children)
                {
                    dic.Add(child.Key, JsonUtility.FromJson<T>(child.GetRawJsonValue()));
                }
            }

            result = new FirebaseResult<Dictionary<string, T>>(dic);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif
            result = new FirebaseResult<Dictionary<string, T>>(false);
        }

        return result;
    }

    public static async Task<FirebaseResult<Dictionary<string, T>>> GetKeyValuesAsync<T>(string path)
    {
        FirebaseResult<Dictionary<string, T>> result;

        try
        {
            DataSnapshot ss = await GetReference(path).GetValueAsync();

            Dictionary<string, T> dic = null;

            // check there are valid data
            if (ss.Exists && ss.ChildrenCount > 0)
            {
                dic = new Dictionary<string, T>();

                foreach (var child in ss.Children)
                {
                    dic.Add(child.Key, (T)child.GetValue(false));
                }
            }

            result = new FirebaseResult<Dictionary<string, T>>(dic);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif
            result = new FirebaseResult<Dictionary<string, T>>(false);
        }

        return result;
    }

    public static async Task<FirebaseResult<T>> GetValueAsync<T>(string path)
    {
        FirebaseResult<T> result;

        try
        {
            DataSnapshot ss = await GetReference(path).GetValueAsync();

            result = new FirebaseResult<T>(ss.Exists ? (T)ss.GetValue(false) : default(T));
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif
            result = new FirebaseResult<T>(false);
        }

        return result;
    }

    public static async Task<FirebaseResult<List<T>>> GetValuesAsync<T>(string path)
    {
        FirebaseResult<List<T>> result;

        try
        {
            DataSnapshot ss = await GetReference(path).GetValueAsync();

            List<T> values = null;

            // check there are valid data
            if (ss.Exists && ss.ChildrenCount > 0)
            {
                values = new List<T>((int)ss.ChildrenCount);

                foreach (var child in ss.Children)
                {
                    values.Add((T)child.GetValue(false));
                }
            }

            result = new FirebaseResult<List<T>>(values);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif
            result = new FirebaseResult<List<T>>(false);
        }

        return result;
    }

    public static async Task<FirebaseResult> RemoveAsync(string path)
    {
        FirebaseResult result;

        try
        {
            await GetReference(path).RemoveValueAsync();

            result = new FirebaseResult(true);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError(path);
            Debug.LogException(ex);
#endif
            result = new FirebaseResult(false);
        }

        return result;
    }

    public static void RemoveListener(ChildListenerType type, string path, EventHandler<ChildChangedEventArgs> action)
    {
        switch (type)
        {
            case ChildListenerType.Added:
                GetReference(path).ChildAdded -= action;
                break;

            case ChildListenerType.Removed:
                GetReference(path).ChildRemoved -= action;
                break;

            case ChildListenerType.Moved:
                GetReference(path).ChildMoved -= action;
                break;

            case ChildListenerType.Changed:
                GetReference(path).ChildChanged -= action;
                break;
        }
    }

    public static void RemoveListener(ValueListenerType type, string path, EventHandler<ValueChangedEventArgs> action)
    {
        switch (type)
        {
            case ValueListenerType.Changed:
                GetReference(path).ValueChanged -= action;
                break;
        }
    }

    public static async Task<FirebaseResult> SetJsonAsync<T>(string path, T value)
    {
        FirebaseResult result;

        try
        {
            await GetReference(path).SetRawJsonValueAsync(JsonUtility.ToJson(value));

            result = new FirebaseResult(true);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif
            result = new FirebaseResult(false);
        }

        return result;
    }

    public static void SetPersistenceEnabled(bool value)
    {
        _db.SetPersistenceEnabled(value);
    }

    public static async Task<FirebaseResult> SetValueAsync(string path, object value)
    {
        FirebaseResult result;

        try
        {
            await GetReference(path).SetValueAsync(value);

            result = new FirebaseResult(true);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif
            result = new FirebaseResult(false);
        }

        return result;
    }

    public static async Task<FirebaseResult> SetValuesAsync(IDictionary<string, object> update)
    {
        FirebaseResult result;

        try
        {
            await GetReference("/").UpdateChildrenAsync(update);

            result = new FirebaseResult(true);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif
            result = new FirebaseResult(false);
        }

        return result;
    }
    #endregion

    #region Functions
    private static readonly FirebaseFunctions _functions = FirebaseFunctions.DefaultInstance;

    public static async Task<FirebaseResult<HttpsCallableResult>> CallAsync(string name, Dictionary<string, object> data)
    {
        FirebaseResult<HttpsCallableResult> result;

        try
        {
            var https = await _functions.GetHttpsCallable(name).CallAsync(data);

            result = new FirebaseResult<HttpsCallableResult>(https);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif
            result = new FirebaseResult<HttpsCallableResult>(false);
        }

        return result;
    }
    #endregion

    #region Messaging
    public static void AddTokenReceivedListener(EventHandler<TokenReceivedEventArgs> OnTokenReceived)
    {
        FirebaseMessaging.TokenReceived += OnTokenReceived;
    }

    public static async Task<FirebaseResult<string>> GetTokenAsync()
    {
        FirebaseResult<string> result;

        try
        {
            var token = await FirebaseMessaging.GetTokenAsync();

            result = new FirebaseResult<string>(token);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif
            result = new FirebaseResult<string>(false);
        }

        return result;
    }
    #endregion

    #region Storage
    private static readonly FirebaseStorage _storage = FirebaseStorage.DefaultInstance;

    public static async Task<FirebaseResult> DeleteFileAsync(string path)
    {
        FirebaseResult result;

        try
        {
            // delete file
            await _storage.GetReference(path).DeleteAsync();

            result = new FirebaseResult(true);
        }
        catch (Exception ex)
        {
            // if there's no file at specified path, return success result
            if (ex is StorageException storageEx && storageEx.ErrorCode == StorageException.ErrorObjectNotFound)
            {
                result = new FirebaseResult<StorageMetadata>(true);
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogException(ex);
#endif

                result = new FirebaseResult<StorageMetadata>(false);
            }
        }

        return result;
    }

    public static async Task<FirebaseResult<byte[]>> DownloadBytesAsync(string path, long maxSize = long.MaxValue)
    {
        FirebaseResult<byte[]> result;

        try
        {
            // download file as byte array
            var data = await _storage.GetReference(path).GetBytesAsync(maxSize);

            result = new FirebaseResult<byte[]>(data);
        }
        catch (Exception ex)
        {
            // if there's no file at specified path, return success result
            if (ex is StorageException storageEx && storageEx.ErrorCode == StorageException.ErrorObjectNotFound)
            {
                result = new FirebaseResult<byte[]>(true);
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogException(ex);
#endif

                result = new FirebaseResult<byte[]>(false);
            }
        }

        return result;
    }

    public static async Task<FirebaseResult> DownloadFileAsync(string path, string filePath)
    {
        FirebaseResult result;

        try
        {
            // download file and save it in disk with specified path
            await _storage.GetReference(path).GetFileAsync(filePath);

            result = new FirebaseResult(true);
        }
        catch (Exception ex)
        {
            // if there's no file at specified path, return success result
            if (ex is StorageException storageEx && storageEx.ErrorCode == StorageException.ErrorObjectNotFound)
            {
                result = new FirebaseResult(true);
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogException(ex);
#endif

                result = new FirebaseResult(false);
            }
        }

        return result;
    }

    public static async Task<FirebaseResult<StorageMetadata>> DownloadMetadataAsync(string path)
    {
        FirebaseResult<StorageMetadata> result;

        try
        {
            // download metadata of file
            var metaData = await _storage.GetReference(path).GetMetadataAsync();

            result = new FirebaseResult<StorageMetadata>(metaData);
        }
        catch (Exception ex)
        {
            // if there's no file at specified path, return success result
            if (ex is StorageException storageEx && storageEx.ErrorCode == StorageException.ErrorObjectNotFound)
            {
                result = new FirebaseResult<StorageMetadata>(true);
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogException(ex);
#endif

                result = new FirebaseResult<StorageMetadata>(false);
            }
        }

        return result;
    }

    public static async Task<FirebaseResult<Stream>> DownloadStreamAsync(string path)
    {
        FirebaseResult<Stream> result;

        try
        {
            // download file as stream
            var stream = await _storage.GetReference(path).GetStreamAsync();

            result = new FirebaseResult<Stream>(stream);
        }
        catch (Exception ex)
        {
            // if there's no file at specified path, return success result
            if (ex is StorageException storageEx && storageEx.ErrorCode == StorageException.ErrorObjectNotFound)
            {
                result = new FirebaseResult<Stream>(true);
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogException(ex);
#endif

                result = new FirebaseResult<Stream>(false);
            }
        }

        return result;
    }

    public static async Task<FirebaseResult<Uri>> DownloadUriAsync(string path)
    {
        FirebaseResult<Uri> result;

        try
        {
            // download file as uri
            var uri = await _storage.GetReference(path).GetDownloadUrlAsync();

            result = new FirebaseResult<Uri>(uri);
        }
        catch (Exception ex)
        {
            // if there's no file at specified path, return success result
            if (ex is StorageException storageEx && storageEx.ErrorCode == StorageException.ErrorObjectNotFound)
            {
                result = new FirebaseResult<Uri>(true);
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogException(ex);
#endif

                result = new FirebaseResult<Uri>(false);
            }
        }

        return result;
    }

    public static async Task<FirebaseResult> UploadBytesAsync(string path, byte[] data, MetadataChange metadata = null)
    {
        FirebaseResult result;

        try
        {
            // upload byte array
            await _storage.GetReference(path).PutBytesAsync(data, metadata);

            result = new FirebaseResult(true);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif

            result = new FirebaseResult(false);
        }

        return result;
    }

    public static async Task<FirebaseResult> UploadFileAsync(string path, string filePath, MetadataChange metadata = null)
    {
        FirebaseResult result;

        try
        {
            // upload file at specified path
            await _storage.GetReference(path).PutFileAsync(filePath, metadata);

            result = new FirebaseResult(true);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif

            result = new FirebaseResult(false);
        }

        return result;
    }

    public static async Task<FirebaseResult> UploadStreamAsync(string path, Stream stream, MetadataChange metadata = null)
    {
        FirebaseResult result;

        try
        {
            // upload stream
            await _storage.GetReference(path).PutStreamAsync(stream, metadata);

            result = new FirebaseResult(true);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(ex);
#endif

            result = new FirebaseResult(false);
        }

        return result;
    }
    #endregion
}

public class FirebaseResult
{
    public bool CompletedSuccessfully { get; protected set; }
    public int ErrorCode { get; protected set; }

    public FirebaseResult(bool completedSuccessfully, int errorCode = -1)
    {
        CompletedSuccessfully = completedSuccessfully;
        ErrorCode = errorCode;
    }
}

public class FirebaseResult<T> : FirebaseResult
{
    public T Value { get; private set; }

    public FirebaseResult(bool completedSuccessfully) : base(completedSuccessfully)
    {

    }

    public FirebaseResult(int errorCode) : base(false, errorCode)
    {

    }

    public FirebaseResult(T value) : base(true)
    {
        CompletedSuccessfully = true;
        Value = value;
    }

    public FirebaseResult(bool completedSuccessfully, T value) : base(completedSuccessfully)
    {
        CompletedSuccessfully = completedSuccessfully;
        Value = value;
    }
}