using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField] private SaveData saveManager;
    public SaveData SaveDataManager => saveManager;

    public static event Action<float> OnSetVelocity;
    public static event Action<bool> OnEnableToStart;

    [SerializeField] private GameObject gameOverCanvas;
    public GameObject GameOverCanvas => gameOverCanvas;
    
    int highScore = 0;
    float velocity = 1.3f;
    float timerStart = 3f;
    [SerializeField] TextMeshProUGUI countDownTxt;

    public TMP_InputField pseudoInputField;

    [SerializeField] private Canvas pseudoCanvas;
    public Canvas PseudoCanvas => pseudoCanvas;

    [SerializeField] private TextMeshProUGUI popUpErrorTxt;
    [SerializeField] private TextMeshProUGUI showPseudo;
    [SerializeField] private TextMeshProUGUI currentScoreTxt;
    [SerializeField] private TextMeshProUGUI highScoreTxt;

    [SerializeField] private TextMeshProUGUI leaderBoardTxt;
    [SerializeField] private TextMeshProUGUI leaderBoardGOTxt;

    string pseudo;
    private int actualScore;
    public bool canSavePreviousPseudo = true;
    private bool isCroissant = true;
    private bool thisMonth = false;

    void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
    }

    void Start()
    {
        if(!canSavePreviousPseudo)
        {
            PlayerPrefs.DeleteAll();
        }
        
        //Vérifie si un pseudo à déjà jouer sur cet appareil et le "reconnecte" si oui
        if (PlayerPrefs.HasKey("SavedPseudo"))
        {
            pseudo = PlayerPrefs.GetString("SavedPseudo");
            pseudoCanvas.enabled = false; // Cache l'écran de connexion
            //StartCoroutine(GetAndDisplayLeaderboard()); // Récupère et affiche les scores
            StartCoroutine(StartCountdown()); // Lancer directement le jeu
        }
        else
        {
            pseudoCanvas.enabled = true;
            StartCoroutine(GetAndDisplayLeaderboard());
        }
    }

    public void CreatePseudo()
    {
        pseudo = pseudoInputField.text.Trim(); // Récupère le pseudo et enlève les espaces inutiles
        if (string.IsNullOrEmpty(pseudo))
        {
            Debug.LogWarning("Le pseudo ne peut pas être vide !");
            return;
        }

        StartCoroutine(CheckPseudoExists(pseudo));
    }

    // Vérif de l'existance du pseudo
    private IEnumerator CheckPseudoExists(string _pseudo)
    {
        bool exists = false;
        yield return StartCoroutine(saveManager.CheckPseudoExists(_pseudo, (result) => exists = result)); // Retourne exist en vrai ou faux

        if (exists)
        {
            popUpErrorTxt.text = "Le pseudo existe déjà, veuillez vous connecter.";
        }
        else
        {
            OnStartGame();
        }
    }

    public void Login()
    {
        pseudo = pseudoInputField.text.Trim(); // Récupère le pseudo et enlève les espaces inutiles
        if (string.IsNullOrEmpty(pseudo))
        {
            Debug.LogWarning("Le pseudo ne peut pas être vide !");
            return;
        }

        StartCoroutine(CheckPseudoExistsForLogin(pseudo));
    }

    private IEnumerator CheckPseudoExistsForLogin(string _pseudo)
    {
        bool exists = false;
        yield return StartCoroutine(saveManager.CheckPseudoExists(_pseudo, (result) => exists = result));

        if (exists)
        {
            OnStartGame();
        }
        else
        {
            popUpErrorTxt.text = "Le pseudo n'existe pas, veuillez le créer.";
        }
    }

    private void OnStartGame()
    {
        popUpErrorTxt.text = "";

        PlayerPrefs.SetString("SavedPseudo", pseudo); // Sauvegarde du pseudo pour la prochaine partie

        pseudoCanvas.enabled = false;
        StartCoroutine(StartCountdown());
    }

    public void GameOver()
    {
        gameOverCanvas.SetActive(true);

        //Time.timeScale = 0f;
        OnEnableToStart?.Invoke(false);

        StartCoroutine(GetAndDisplayLeaderboard());
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    //Démarre un compte à rebour de 3s avant que le jeu ne commence
    private IEnumerator StartCountdown()
    {
        yield return StartCoroutine(saveManager.GetHighScoreOfThisPlayer(pseudo, (score) => highScore = score));

        highScoreTxt.text = highScore.ToString();
        float timeRemaining = timerStart;
        countDownTxt.enabled = true;

        while (timeRemaining > 0)
        {
            countDownTxt.text = timeRemaining.ToString("F0"); // Affiche sans décimales
            yield return new WaitForSeconds(1f);
            timeRemaining--;
        }

        countDownTxt.text = "GO!";
        yield return new WaitForSeconds(0.5f);

        countDownTxt.enabled = false;

        Time.timeScale = 1f;

        OnSetVelocity?.Invoke(velocity);
        OnEnableToStart?.Invoke(true);

        showPseudo.text = pseudo;
    }

    public void UpdateScore()
    {
        actualScore++;
        currentScoreTxt.text = actualScore.ToString();
        StartCoroutine(UpdateHighScore());
    }

    private IEnumerator UpdateHighScore()
    {
        if(actualScore > highScore)
        {
            yield return StartCoroutine(saveManager.SendScore(pseudo, actualScore));
            highScoreTxt.text = actualScore.ToString();
        }
    }

    public void ChangePseudo()
    {
        PlayerPrefs.DeleteKey("SavedPseudo");
        RestartGame();
    }

    public void OnToogleChanged(bool _isOn)
    {
        isCroissant = _isOn;
        StartCoroutine(GetAndDisplayLeaderboard());
    }

    public void OnToogleDateChanged(bool _isOn)
    {
        thisMonth = _isOn;
        StartCoroutine(GetAndDisplayLeaderboard());
    }

    //Gestion du leaderBoard. Par défaut croissant
    private IEnumerator GetAndDisplayLeaderboard()
    {
        List<PlayerData> sortedPlayers = new List<PlayerData>();
        yield return StartCoroutine(saveManager.GetHighScores(isCroissant, (result) => sortedPlayers = result));

        if(thisMonth)
        {
            DateTime currentMonth = DateTime.Now;
            sortedPlayers = sortedPlayers.Where(player =>
                DateTime.TryParse(player.date, out DateTime parsedDate) &&
                parsedDate.Month == currentMonth.Month &&
                parsedDate.Year == currentMonth.Year).ToList();
        }

        leaderBoardTxt.text = "";
        int rank = 1;
        foreach (var player in sortedPlayers) //Mets les players par ordre vis-à-vis de leur score
        {
            if (DateTime.TryParse(player.date, out DateTime parsedDate))
            {
                string formattedDate = parsedDate.ToString("dd/MM/yyyy");
                leaderBoardTxt.text += $"{rank}. {player.pseudo} - {player.highscore} : {formattedDate}\n";
            }
            else
            {
                leaderBoardTxt.text += $"{rank}. {player.pseudo} - {player.highscore} : Date invalide\n";
            }
            rank++;
        }
        leaderBoardGOTxt.text = leaderBoardTxt.text;
    }

    //TEST
    public void TestConnection()
    {
        StartCoroutine(saveManager.TestServerConnection());
    }

    void OnDestroy()
    {
        instance = null;
    }
}
