using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ScoreManager : MonoBehaviour
{
    public TMP_Text scoreText;
    
    private int score = 0;

    private void OnTriggerEnter(Collider other)
    {
        var ball = other.GetComponent<Pickable>();
        if (ball == null || ball.IsHeld())
        {
            return;
        }

        score++;

        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }
}
