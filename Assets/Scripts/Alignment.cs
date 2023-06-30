using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/* How to align the virtual scene with the physical environment:
At the start of the app, place the controller where the reference point A should be,
press button 1, place the controller where the reference point B should be, press button 1.
The scene should be aligned. Press button 1 again to stop the alignment and hide the reference 
points, or button 2 to start again. */


public class Alignment : MonoBehaviour
{
    /* Attach this script to an empty gameObject that is the parent of all the objects that
    should be impacted by the alignment, including the reference points.
    The XR camera / controllers shouldn't be a children of that object, as it's position doesnt
    depend on the alignment.*/

    /* Create two gameObjects that will be used as reference points (eg. a small sphere for point A and
    a small cube for point B), and place them in the scene, at a position that is easy to locate in 
    the physical environment (eg. corners of the room or corners of a table)*/
    [SerializeField]
    private GameObject referencePointA;

    [SerializeField]
    private GameObject referencePointB;

    // Controller that will be used to make the alignment
    [SerializeField]
    private GameObject controller;

    // Input 
    [SerializeField]
    private InputActionProperty button1;
    [SerializeField]
    private InputActionProperty button2;

    // Positions of the reference points
    private Vector3 virtualPositionA;
    private Vector3 virtualPositionB;

    // Positions where the reference points should be
    private Vector3 realPositionA;
    private Vector3 realPositionB;

    // Progress of the alignment
    private int alignmentState = 0;

    private float previousButton1State;
    private float previousButton2State;

    // Start is called before the first frame update
    void Start()
    {
        virtualPositionA = referencePointA.transform.position; 
        virtualPositionB = referencePointB.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (alignmentState==0){
            // Start of the alignment
            // When button 1 pressed, get the position where the reference point A should be
            if (button1.action.ReadValue<float>()==1 && previousButton1State==0){
                
                realPositionA = controller.transform.position;
                Debug.Log("Point A set");
                
                alignmentState = 1;
            }
        }

        else if (alignmentState==1){
            // When button 1 pressed again, get the position where the reference point B should be
            if (button1.action.ReadValue<float>()==1 && previousButton1State==0){
                
                realPositionB = controller.transform.position;
                Debug.Log("Point B set");

                // A little bit of geometry to rotate, translate and rescale the parent of the objects,
                // to make the reference points match. 
                //Note that the rotation should always be done before the translation and rescaling.

                // Rotate
                Vector3 virtualVector = virtualPositionB - virtualPositionA;
                Vector3 virtualVectorXZ = new Vector3(virtualVector.x, 0, virtualVector.z);
                Vector3 realVector = realPositionB - realPositionA;
                Vector3 realVectorXZ = new Vector3(realVector.x, 0, realVector.z);
                Quaternion rotationOffset = Quaternion.FromToRotation(virtualVectorXZ, realVectorXZ);
                transform.rotation = rotationOffset * transform.rotation;
                virtualPositionA = referencePointA.transform.position; 
                virtualPositionB = referencePointB.transform.position;

                // Rescale 
                float scaleFactor = (realPositionB-realPositionA).magnitude / (virtualPositionB-virtualPositionA).magnitude;
                transform.localScale =  transform.localScale * scaleFactor;
                virtualPositionA = referencePointA.transform.position; 
                virtualPositionB = referencePointB.transform.position;

                // Translate 
                transform.position = transform.position + (realPositionA-virtualPositionA);

                Debug.Log("Alignment done, push button 1 again to confirm, button 2 to restart");
                
                alignmentState = 2;
            }
        }

        else if (alignmentState==2){
            // The alignment is done.
            // When button 1 pressed, stop and hide reference points
            if (button1.action.ReadValue<float>()==1 && previousButton1State==0){
                referencePointA.SetActive(false);
                referencePointB.SetActive(false);

                alignmentState = 3;
            }

            // When button 2 pressed, start again
            if (button2.action.ReadValue<float>()==1 && previousButton2State==0){
                virtualPositionA = referencePointA.transform.position;
                virtualPositionB = referencePointB.transform.position;
            
                alignmentState = 0;
            }
        }

        previousButton1State = button1.action.ReadValue<float>();
        previousButton2State = button2.action.ReadValue<float>();
    }
}
