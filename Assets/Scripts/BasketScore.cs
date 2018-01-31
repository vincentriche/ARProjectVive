using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasketScore : MonoBehaviour
{
    public static BasketScore Instance;
    public TextMesh scoreText;

    bool direction = true;
    private Vector3 position1 = new Vector3(-0.327f, -0.6166562f, -1.518067f);
    private Vector3 position2 = new Vector3(-4.68f, -0.6166562f, -1.518067f);
    private int currentScore = 0;


    void Awake ()
    {
        Instance = this;
	}

    public void Update()
    {

        if(direction == true)
            transform.position = Vector3.Lerp(transform.position, position1, Time.deltaTime / 2.0f);
        else
            transform.position = Vector3.Lerp(transform.position, position2, Time.deltaTime / 2.0f);

        if (Vector3.Distance(transform.position, position1) < 1.0f)
            direction = false;

        if (Vector3.Distance(transform.position, position2) < 1.0f)
            direction = true;
    }

    public void IncreaseScore()
    {
        currentScore++;
        scoreText.text = currentScore.ToString();
    }

    public void OnTriggerEnter(Collider other)
    {
        IncreaseScore();
    }
}
