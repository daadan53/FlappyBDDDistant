using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

public class ServConnectTest
{
    private string baseUrl = "http://flapybdd.alwaysdata.net/highscores.php?action=get";

    [UnityTest]
    public IEnumerator DatabaseConnectionTest()
    {
        UnityWebRequest request = UnityWebRequest.Get(baseUrl);
        yield return request.SendWebRequest();

        Assert.AreEqual(UnityWebRequest.Result.Success, request.result, "La connexion à la base de données a échoué : " + request.error);

        string responseTxt = request.downloadHandler.text;
        Assert.IsNotNull(responseTxt, "La réponse de la BDD est null.");
        Assert.IsNotEmpty(responseTxt, "La réponse de la BDD est vide.");
    }
}
