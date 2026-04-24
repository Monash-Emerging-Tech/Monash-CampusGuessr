using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class APIManager : MonoBehaviour
{
    public static APIManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // GET Request
    public void Get(string url, Action<string> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(GetRequest(url, onSuccess, onError));
    }

    private IEnumerator GetRequest(string url, Action<string> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                onSuccess?.Invoke(request.downloadHandler.text);
            else
                onError?.Invoke(request.error);
        }
    }

    // POST Request (JSON)
    public void Post(string url, string jsonBody, Action<string> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(PostRequest(url, jsonBody, onSuccess, onError));
    }

    private IEnumerator PostRequest(string url, string jsonBody, Action<string> onSuccess, Action<string> onError)
    {
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                onSuccess?.Invoke(request.downloadHandler.text);
            else
                onError?.Invoke(request.error);
        }
    }

    public void Get<T>(string url, Action<T> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(GetRequest(url, onSuccess, onError));
    }

    private IEnumerator GetRequest<T>(string url, Action<T> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                T data = JsonUtility.FromJson<T>(request.downloadHandler.text);
                onSuccess?.Invoke(data);
            }
            else
            {
                onError?.Invoke(request.error);
            }
        }
    }
}
