using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;
using System.Collections.Generic;

public class AddAndRemovePlayerTest
{
    private SaveData saveData;
    private string testPlayerName = "TestPlayer_UnitTest";

    [SetUp]
    public void SetUp()
    {
        GameObject mongoManagerObject = new GameObject();
        saveData = mongoManagerObject.AddComponent<SaveData>();
    }

    [UnityTest]
    public IEnumerator AddPlayer_AndVerify_ThenRemove()
    {
        // Ajouter un joueur
        yield return saveData.StartCoroutine(saveData.SendScore(testPlayerName, 0));

        // Vérifier que le joueur existe bien
        List<PlayerData> retrievedPlayers = null;
        yield return saveData.StartCoroutine(saveData.GetHighScores(true, (result) => retrievedPlayers = result));

        PlayerData foundPlayer = retrievedPlayers?.Find(p => p.pseudo == testPlayerName);
        Assert.IsNotNull(foundPlayer, "Le joueur n'a pas été trouvé après l'ajout.");
        Assert.AreEqual(testPlayerName, foundPlayer.pseudo, "Le pseudo du joueur ne correspond pas.");

        // Supprimer le joueur après le test
        yield return DeleteTestPlayerFromDatabase(testPlayerName);
    }

    private IEnumerator DeleteTestPlayerFromDatabase(string playerName)
    {
        /*WWWForm form = new WWWForm();
        form.AddField("name", playerName);*/

        using (UnityWebRequest request = UnityWebRequest.Get("http://flapybdd.alwaysdata.net/highscores.php?action=delete&pseudo=" + UnityWebRequest.EscapeURL(playerName)))
        {
            yield return request.SendWebRequest();

            Assert.AreEqual(UnityWebRequest.Result.Success, request.result, "La suppression du joueur a échoué : " + request.error);
        }
    }
}
