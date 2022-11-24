using System.Collections;
using System.Collections.Generic;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class CmdTest : MonoBehaviour
{

    public Text showInput;
    public Text showOutput;
    public Text showReceive;
    public string testCommandLine;

    System.Collections.Specialized.NameValueCollection parameters;

    void Start()
    {
        string commandLineRaw = Environment.CommandLine;
#if UNITY_EDITOR
        commandLineRaw = testCommandLine;
#endif
        var commandLine = HttpUtility.UrlDecode(commandLineRaw);
        print($"【CommandLine】{commandLineRaw}");
        print($"【CommandLine-UrlDecode】{commandLine}");

        string urlString = Tool.MakeFakeURL(commandLine);
        var url = new Uri(urlString);

        parameters = HttpUtility.ParseQueryString(url.Query);
        string parameterString = "";
        foreach (string key in parameters.AllKeys)
        {
            parameterString += $"{key}: {parameters[key]}\n";
        }
        showInput.text = parameterString;

        StartCoroutine(HttpGet());

    }

    IEnumerator HttpGet()
    {
        string accesKey = "HyWJbtycfTXTT8KV";
        string accessSecret = "gKVv9ITutak77xoWDWnFxAXMRH3FlYhk";
        string timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds().ToString();
        string signatureContent =
            "accesKey=" + accesKey
            + "&" + "appendix=" + parameters["appendix"]
            + "&" + "timestamp=" + timestamp
            + "&" + "userId=" + parameters["userId"];
        string signature = Tool.HmacSHA256(signatureContent, accessSecret);

        string urlPart1 = parameters["serverAddress"] + "/openApi/vr/v2/openAck.json" + "?";
        string urlPart2 =
            "accesKey=" + accesKey
            + "&" + "appendix=" + HttpUtility.UrlEncode(parameters["appendix"])
            + "&" + "timestamp=" + timestamp
            + "&" + "userId=" + parameters["userId"]
            + "&" + "signature=" + HttpUtility.UrlEncode(signature);
        string url = urlPart1 + urlPart2;
        print($"【url】{url}");
        print($"【url-UrlDecode】{HttpUtility.UrlDecode(url)}");
        showOutput.text = HttpUtility.UrlDecode(url);
        {
            UnityWebRequest request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();
            if (request.isHttpError || request.isNetworkError)
            {
                Debug.LogError(request.error);
            }
            else
            {
                string receiveContent = request.downloadHandler.text;
                Debug.Log(receiveContent);
                showReceive.text = receiveContent;
            }
        }

    }
}

static class Tool
{
    static public string HmacSHA256(string secret, string signKey)
    {
        string signRet = string.Empty;
        using (HMACSHA256 mac = new HMACSHA256(Encoding.UTF8.GetBytes(signKey)))
        {
            byte[] hash = mac.ComputeHash(Encoding.UTF8.GetBytes(secret));
            signRet = Convert.ToBase64String(hash);
        }
        return signRet;
    }

    /// <summary>
    /// 找到第一个空格，
    /// 找到它之后的第一个冒号
    /// 改成问号
    /// </summary>
    /// <param name="text"></param>
    /// <param name="search"></param>
    /// <param name="replace"></param>
    /// <returns></returns>
    static public string ReplaceWithQuestionMark(string text)
    {
        int pos = text.IndexOf(" ");
        if (pos < 0)
        {
            pos = 0;
        }

        int pos2 = text.IndexOf(":", pos);
        if (pos2 < 0)
        {
            return text;
        }

        return text.Substring(0, pos2) + "?" + text.Substring(pos2 + 1);
    }

    static public string MakeFakeURL(string text)
    {
        text = ReplaceWithQuestionMark(text);

        int pos = text.IndexOf("?");
        if (pos < 0)
        {
            return text;
        }
        return "https://www.123.com/" + text.Substring(pos);
    }
}
