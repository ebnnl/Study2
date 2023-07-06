using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ResetGame : MonoBehaviour
{
    public GameObject racket;
    public GameObject ball;

    Vector3 racketInitialPos;
    Vector3 ballInitialPos;
    Quaternion racketInitialRot;
    Quaternion ballInitialRot;

    [SerializeField]
    private InputActionProperty controllerButton1;


    // Start is called before the first frame update
    void Start()
    {
        racketInitialPos = racket.transform.localPosition;
        racketInitialRot = racket.transform.localRotation;
        ballInitialPos = ball.transform.localPosition;
        ballInitialRot = ball.transform.localRotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (controllerButton1.action.ReadValue<float>() == 1)
        {
            racket.transform.localPosition = racketInitialPos;
            racket.transform.localRotation = racketInitialRot;
            ball.transform.localPosition = ballInitialPos;
            ball.transform.localRotation = ballInitialRot;
        }
    }
}
