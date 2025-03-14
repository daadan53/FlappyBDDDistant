using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using SimpleJSON;
using System.Linq;

//L'objet player
[Serializable]
public class SaveDataList
{
    public List<PlayerData> players;
}

public class SaveData : MonoBehaviour
{
    private const string BASEURL = "http://flapybdd.alwaysdata.net/highscores.php?action=";
    
    //IEnumerator car unity web request est une requete asynchrone
    public IEnumerator SendScore(string _pseudo, int _highscore)
    {
        WWWForm form = new WWWForm();
        form.AddField("pseudo", _pseudo);
        form.AddField("highscore", _highscore);

        using (UnityWebRequest www = UnityWebRequest.Post(BASEURL + "add", form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
                Debug.Log("Score envoyé !");
            else
                Debug.LogError("Erreur : " + www.error);
        }
    }

    public IEnumerator GetHighScores(bool _isCroissant, Action<List<PlayerData>> _callback)
    {

        string url = BASEURL + "get";
        //if (_thisMonth) url += "&month=" + DateTime.Now.Month + "&year=" + DateTime.Now.Year; //Récupère seulement ce mois-ci ou non

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                List<PlayerData> scores = JsonUtility.FromJson<SaveDataList>("{\"players\":" + www.downloadHandler.text + "}").players;
                
                if (_isCroissant)
                {
                    scores = scores.OrderBy(p => p.highscore).ToList(); // On trie par odre croissant 
                }
                else
                {
                    scores = scores.OrderByDescending(p => p.highscore).ToList(); // On trie par ordre décroissant
                }
        
                _callback(scores);
            }
            else
            {
                Debug.LogError("Erreur : " + www.error);
            }
        }
    }

    public IEnumerator GetHighScoreOfThisPlayer(string _pseudo, Action<int> _callback)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(BASEURL + "get&pseudo=" + UnityWebRequest.EscapeURL(_pseudo)))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string responseTxt = www.downloadHandler.text;
                Debug.Log(responseTxt);
                
                var json = JSON.Parse(responseTxt); // On parse la réponse du serveur pour en faire une liste

                if (json.Count > 0)
                {
                    int highScore = json[0]["highscore"].AsInt; // On récupère seulement la première clé du tableau et seulement highscore 
                    _callback(highScore);
                }
                else
                {
                    Debug.LogWarning("Aucun meilleur score trouvé !");
                    _callback(0); // Si le score n'est pas trouvé, retourne 0
                }
            }
            else
            {
                Debug.LogError("Erreur de requête : " + www.error);
                _callback(0); // En cas d'erreur, retourne 0
            }
        }
    }

    public IEnumerator CheckPseudoExists(string _pseudo, Action<bool> _callback)
    {
        string url = BASEURL + "get&pseudo=" + UnityWebRequest.EscapeURL(_pseudo); //Envoie le pseudo de manière encodé

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string json = webRequest.downloadHandler.text;
                Debug.Log("Réponse du serveur: " + json);

                if (!string.IsNullOrEmpty(json) && json != "[]") 
                {
                    _callback(true);
                }
                else 
                {
                    _callback(false);
                }
            }
            else
            {
                Debug.LogError("Erreur de connexion: " + webRequest.error);
                _callback(false);
            }
        }
    }

    //TEST
    public IEnumerator TestServerConnection()
    {
        using (UnityWebRequest www = UnityWebRequest.Get("http://flapybdd.alwaysdata.net/highscores.php?action=get"))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
                Debug.Log("Réponse du serveur : " + www.downloadHandler.text);
            else
                Debug.LogError("Erreur : " + www.error);
        }
    }
}
