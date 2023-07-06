using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using TMPro;

public class MainManager : MonoBehaviour, Photon.Pun.IPunObservable
{

    [SerializeField]
    private int participantID = 0;

    [SerializeField]
    private StudyManager studyManager;

    [SerializeField]
    private StudyManager trainingStudyManager;

    [SerializeField]
    private Camera participantCamera;
    [SerializeField]
    private Camera desktopCamera;

    [SerializeField]
    private InputActionProperty controllerButton1;

    [SerializeField]
    private PhotonView participantPhotonView;

    // States
    public const int MENU = 0;
    public const int TRAINING = 1;
    public const int STUDY = 3;

    private int currentState = 0;
    private int previousState = 0;

    [SerializeField]
    private PhotonView photonView;

    [SerializeField]
    private GameObject mainMenu;

    [SerializeField]
    private Button startTrainingButton;

    [SerializeField]
    private Button startStudyButton;

    [SerializeField]
    private TMP_InputField participantInput;

    private bool ownerSetup = false;
    

    // Start is called before the first frame update
    void Start()
    {
        participantCamera.enabled = true;
        desktopCamera.enabled = false;
        //studyManager.SetVolume(0);
        //trainingStudyManager.SetVolume(0);

        currentState = 0;

        mainMenu.SetActive(true);
        startTrainingButton.interactable = false;
        startStudyButton.interactable = false;

        startTrainingButton.onClick.AddListener(StartTraining);
        startStudyButton.onClick.AddListener(StartStudy);

        // Set initial ownership of the shared object
        if (photonView.IsMine)
        {
            photonView.TransferOwnership(PhotonNetwork.LocalPlayer);
        }
    }

    // Update is called once per frame
    void Update()
    {

        if(Input.GetKeyUp("space") && !ownerSetup){
            participantCamera.enabled = false;
            desktopCamera.enabled = true;
            //studyManager.SetVolume(0.5f);
            //trainingStudyManager.SetVolume(0.5f);
            if (!photonView.IsMine)
            {
                photonView.TransferOwnership(PhotonNetwork.LocalPlayer);
            }
            ownerSetup = true;
        }

        /*if (controllerButton1.action.ReadValue<float>() == 1 && !ownerSetup)
        {
            if (!participantPhotonView.IsMine)
            {
                participantPhotonView.TransferOwnership(PhotonNetwork.LocalPlayer);
            }
            ownerSetup = true;
        }*/

        if (Input.GetKeyUp("t")){
            StartTraining();
        }
        if(Input.GetKeyUp("s")){
            StartStudy();
        }

        if(currentState==MENU && !string.IsNullOrWhiteSpace(participantInput.text)){
            int.TryParse(participantInput.text, out participantID);
            startStudyButton.interactable = true;
            startTrainingButton.interactable = true;
        }
        else if (currentState==MENU && string.IsNullOrWhiteSpace(participantInput.text)){
            startStudyButton.interactable = false;
            startTrainingButton.interactable = false;
        }
        

        ManageStateTransitions();
    }

    private void StartTraining(){
        currentState = TRAINING;
        mainMenu.SetActive(false);
    }

    private void StartStudy(){
        currentState = STUDY;
        mainMenu.SetActive(false);
        startStudyButton.gameObject.SetActive(false);
    }

    private void ManageStateTransitions(){
        if(currentState==TRAINING && previousState!=TRAINING){
            trainingStudyManager.gameObject.SetActive(true);
            studyManager.gameObject.SetActive(false);
            trainingStudyManager.SetParticipantID(participantID);
        }

        if(currentState==STUDY && previousState!=STUDY){
            trainingStudyManager.gameObject.SetActive(false);
            studyManager.gameObject.SetActive(true);
            studyManager.SetParticipantID(participantID);
        }

        previousState = currentState;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(currentState);
            stream.SendNext(participantID);
        }
        else if (stream.IsReading){
            currentState = (int)stream.ReceiveNext();
            participantID = (int)stream.ReceiveNext();
            ManageStateTransitions();
        }
            
    }
}
