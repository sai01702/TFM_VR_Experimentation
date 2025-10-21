using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.Windows.Speech; //para usar KeywordRecognizer
using System; //para usar Action
using System.Linq; //para usar ToArray

public class ReconocimientoVoz : MonoBehaviour
{
    public Transform bulletSpawnPoint;
    public float bulletSpeed = 10;
    public TMP_Text scoreText;
    //para reconocimiento de voz
    KeywordRecognizer keywordRecognizer;
    Dictionary<string, Action> wordsToActions;

    void Start()
    {
        wordsToActions = new Dictionary<string, Action>(); //creamos un diccionario de palabras a acciones
        wordsToActions.Add("shoot", Shoot); //agregamos la palabra "shoot" al diccionario y la accion Disparar
        //keywordRecognizer = new KeywordRecognizer(wordsToActions.Keys.ToArray()); //creamos un KeywordRecognizer con las palabras del diccionario convertidas a un array, OLD SCRIPT
        keywordRecognizer = new KeywordRecognizer(wordsToActions.Keys.ToArray(), ConfidenceLevel.Low); //creamos un KeywordRecognizer con las palabras del diccionario convertidas a un array, Also, I specified a low confidence level to improve recognition accuracy.
        keywordRecognizer.OnPhraseRecognized += WordRecognizer; //asignamos el metodo WordRecognizer al evento OnPhraseRecognized del KeywordRecognizer
        keywordRecognizer.Start(); //iniciamos el KeywordRecognizer
    }

    void Update()
    {
        Puntaje();

    }
    private void WordRecognizer(PhraseRecognizedEventArgs word)
    {
        Debug.Log(word.text);
        wordsToActions[word.text].Invoke();
    }
    public void Shoot()
    {
        Debug.Log("Shoot");
        var bullet = PoolManager.Instance.GetBullet();
        bullet.transform.position = bulletSpawnPoint.position;
        bullet.transform.rotation = bulletSpawnPoint.rotation;
        bullet.SetActive(true);
        bullet.GetComponent<Rigidbody>().velocity = bulletSpawnPoint.forward * bulletSpeed;

    }

    void Puntaje()
    { // función para mostrar el puntaje en pantalla
        // GameObject.FindObjectOfType<UnityEngine.UI.Text>().text = "Puntuación  " + ObjetivosManager.Instance.puntos;
        // GameObject.FindWithTag("Score").GetComponent<Text>().text = "Puntuación " + ObjetivosManager.Instance.puntos;
        scoreText.text = "Puntuación " + ObjetivosManager.Instance.puntos;
    }
}
