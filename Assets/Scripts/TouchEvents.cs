﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TouchEvents : MonoBehaviour
{
    private ButtonClicking g_ButtonManager;
    private JSONManager g_JsonManager;

    private GameObject g_LineAnnotationsContainer;
    private GameObject g_IconAnnotationsContainer;
    private GameObject g_NewLine;
    private Material g_LineAnnotationMaterial;

    private List<Vector3> g_CurrentLine;

    private int g_AnnotationCounter;

    public Transform g_SelectedElementTransform { get; set; }

    // Use this for initialization
    void Start ()
    {
        assetLoading();
        assetInitialization();
    }

    // Update is called once per frame
    void Update ()
    {
        // Verify that there was a touch event
        if (EventSystem.current != null && Input.touchCount > 0)
        {
            Touch[] myTouches = Input.touches;

            Vector3 touchedPoint3D = new Vector3(myTouches[0].position.x, myTouches[0].position.y, 0.0f);

            //Raycasting to see if the UI was hit from the camera perspective
            PointerEventData pointer = new PointerEventData(EventSystem.current);
            pointer.position = Camera.main.WorldToScreenPoint(touchedPoint3D);
            List<RaycastResult> raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, raycastResults);

            // The touch was performed on an element that was part of the UI
            if (raycastResults.Count > 0)
            {
                // The touch was performed in one of the Icon annotations
                if (raycastResults[0].gameObject.transform.parent.gameObject.name == "IconAnnotationsContainer")
                {
                    // Single touch interactions
                    if (Input.touchCount == 1)
                    {
                        // As soon as the touch initiates
                        if (myTouches[0].phase == TouchPhase.Began)
                        {
                            // Deselect all the icon annotations
                            foreach (Transform child in g_IconAnnotationsContainer.transform)
                            {
                                g_ButtonManager.ImageDeselected(child.gameObject);
                            }

                            // make the currently touch annotation the selected one, and change its color
                            g_SelectedElementTransform = raycastResults[0].gameObject.transform;
                            g_ButtonManager.SetObjectForColorChange(true, g_SelectedElementTransform);
                        }

                        // The finger is moving through the screen
                        else if (myTouches[0].phase == TouchPhase.Moved)
                        {
                            // Update the position of the annotation that is being dragged
                            if (g_SelectedElementTransform != null)
                            {
                                g_SelectedElementTransform.position = g_SelectedElementTransform.position + new Vector3(myTouches[0].deltaPosition.x, myTouches[0].deltaPosition.y, 0.0f);

                                // Stores the important info of the annotation for the JSON
                                List<float> annotation_information = new List<float>();
                                annotation_information.Add(g_SelectedElementTransform.position.x);
                                annotation_information.Add(g_SelectedElementTransform.position.y);
                                annotation_information.Add(g_SelectedElementTransform.localEulerAngles.z);
                                annotation_information.Add(g_SelectedElementTransform.localScale.x);

                                g_JsonManager.createJSONable(int.Parse(g_SelectedElementTransform.gameObject.GetComponent<Image>().name), "UpdateAnnotationCommand", null, g_SelectedElementTransform.gameObject.GetComponent<Image>().sprite.name, annotation_information);
                            }
                        }
                    }
                    // Multi finger touch performed over an icon annotation
                    // This could just be an else, but we are leaving it open in case of "more-than-two-touch-points" interactions
                    else if (Input.touchCount == 2)
                    {
                        // Find the position in the previous frame of each touch.
                        Vector2 touchZeroPrevPos = myTouches[0].position - myTouches[0].deltaPosition;
                        Vector2 touchOnePrevPos = myTouches[1].position - myTouches[1].deltaPosition;

                        // As soon as the touch initiates
                        if (myTouches[0].phase == TouchPhase.Began)
                        {
                            // Deselect all the icon annotations
                            foreach (Transform child in g_IconAnnotationsContainer.transform)
                            {
                                g_ButtonManager.ImageDeselected(child.gameObject);
                            }

                            // make the currently touch annotation the selected one, and change its color
                            g_SelectedElementTransform = raycastResults[0].gameObject.transform;
                            g_ButtonManager.SetObjectForColorChange(true, g_SelectedElementTransform);
                        }

                        // Zoom interaction
                        else if (myTouches[0].phase == TouchPhase.Moved && myTouches[1].phase == TouchPhase.Moved)
                        {
                            Vector2 prevTouchDelta = touchZeroPrevPos - touchOnePrevPos;
                            Vector2 touchDelta = myTouches[0].position - myTouches[1].position;

                            float angle = Vector2.Angle(prevTouchDelta, touchDelta);

                            // The angle between the vector is higher that the rotation threshold. The event is a rotation
                            if (angle > 0.1)
                            {
                                var LR = Vector3.Cross(prevTouchDelta, touchDelta);
                                if (LR.z > 0)
                                    g_SelectedElementTransform.Rotate(0.0f, 0.0f, angle);
                                else
                                    g_SelectedElementTransform.Rotate(0.0f, 0.0f, -1.0f * angle);
                            }

                            // The event is a pinch. Perform zoom.
                            else
                            {
                                // Find the magnitude of the vector (the distance) between the touches in each frame.
                                float prevTouchDeltaMag = prevTouchDelta.magnitude;
                                float touchDeltaMag = touchDelta.magnitude;

                                // Find the difference in the distances between each frame. The -10 is for scaling purposes
                                float deltaMagnitudeDiff = (prevTouchDeltaMag - touchDeltaMag) / -10.0f;

                                g_SelectedElementTransform.localScale = new Vector3(g_SelectedElementTransform.localScale.x + deltaMagnitudeDiff,
                                    g_SelectedElementTransform.localScale.y + deltaMagnitudeDiff, g_SelectedElementTransform.localScale.z);
                            }
                        }
                        else if (myTouches[0].phase == TouchPhase.Ended && myTouches[1].phase == TouchPhase.Ended)
                        {
                            // Stores the important info of the annotation for the JSON
                            List<float> annotation_information = new List<float>();
                            annotation_information.Add(g_SelectedElementTransform.position.x);
                            annotation_information.Add(g_SelectedElementTransform.position.y);
                            annotation_information.Add(g_SelectedElementTransform.localEulerAngles.z);
                            annotation_information.Add(g_SelectedElementTransform.localScale.x);

                            g_JsonManager.createJSONable(int.Parse(g_SelectedElementTransform.gameObject.GetComponent<Image>().name), "UpdateAnnotationCommand", null, g_SelectedElementTransform.gameObject.GetComponent<Image>().sprite.name, annotation_information);
                        }
                    }
                }
                // UI Interaction. The bulk of these functions is at the ButtonClicking module
                else
                {
                    if (g_SelectedElementTransform != null)
                    {
                        // Deselect all the icon annotations
                        foreach (Transform child in g_IconAnnotationsContainer.transform)
                        {
                            g_ButtonManager.ImageDeselected(child.gameObject);
                        }
                        // Deselect other buttons in the panel that might have been pressed
                        g_ButtonManager.SetObjectForColorChange(false, g_SelectedElementTransform);
                    }
                }
            }
            // Touch interaction on the background image
            else
            {
                // Single touch interaction. These are performed to create line annotations
                if (Input.touchCount == 1)
                {
                    // Record initial touch position.
                    if (myTouches[0].phase == TouchPhase.Began)
                    {
                        // Deselect all the icon annotations
                        foreach (Transform child in g_IconAnnotationsContainer.transform)
                        {
                            g_ButtonManager.ImageDeselected(child.gameObject);
                        }

                        // Initial point for a line annotation
                        if (g_ButtonManager.g_LineButtonClicked)
                        {
                            resetLineAnnotation();
                            g_CurrentLine.Add(touchedPoint3D);
                        }
                    }

                    // Finger is moving through the screen as the line is being created
                    else if (myTouches[0].phase == TouchPhase.Moved)
                    {
                        if (g_ButtonManager.g_LineButtonClicked)
                        {
                            g_CurrentLine.Add(touchedPoint3D);
                            drawLine();
                        }
                    }

                    // The touch event finished
                    else if (myTouches[0].phase == TouchPhase.Ended)
                    {
                        // The touch was done to create an icon annotation. Create an icon annotation at the point of touch
                        if (g_ButtonManager.g_PanelButtonClicked)
                        {
                            string imageName = g_ButtonManager.UnselectPanelButton();
                            createIconAnnotation(touchedPoint3D, imageName);
                        }
                        // The touch has to do with line annotations
                        else
                        {
                            // The touch was the end of a line annotation. Create the line with the previously stores points
                            if (g_ButtonManager.g_LineButtonClicked)
                            {
                                g_CurrentLine.Add(touchedPoint3D);
                                drawLine();
                                g_JsonManager.createJSONable(g_AnnotationCounter, "CreateAnnotationCommand", g_CurrentLine, null, null);
                                g_AnnotationCounter++;
                            }
                            // The touch was done to create a point annotation. Create the point and draw it
                            else if (g_ButtonManager.g_PointsButtonClicked)
                            {
                                resetLineAnnotation();
                                createPointAnnotation(touchedPoint3D);
                                drawLine();
                                g_JsonManager.createJSONable(g_AnnotationCounter, "CreateAnnotationCommand", g_CurrentLine, null, null);
                                g_AnnotationCounter++;
                            }
                        }
                    }
                }
                // Multi touch interaction. These are performed to zoom the background image
                // This could just be an else, but we are leaving it open in case of "more-than-two-touch-points" interactions
                else if (Input.touchCount == 2)
                {

                }
            }
        }
    }

    public void EraseSelected()
    {
        if(g_SelectedElementTransform != null)
        {
            g_JsonManager.createJSONable(int.Parse(g_SelectedElementTransform.gameObject.GetComponent<Image>().name), "DeleteAnnotationCommand", null, null, null);

            Destroy(g_SelectedElementTransform.gameObject);
        }

        g_SelectedElementTransform = null;
    }

    public void EraseAll()
    {
        foreach(Transform child in g_LineAnnotationsContainer.transform)
        {
            g_JsonManager.createJSONable(int.Parse(child.gameObject.name), "DeleteAnnotationCommand", null, null, null);
            Destroy(child.gameObject);
        }

        foreach (Transform child in g_IconAnnotationsContainer.transform)
        {
            g_JsonManager.createJSONable(int.Parse(child.gameObject.GetComponent<Image>().name), "DeleteAnnotationCommand", null, null, null);
            Destroy(child.gameObject);
        }

        g_SelectedElementTransform = null;
    }

    private void assetLoading()
    {
        if (g_LineAnnotationMaterial == null)
        {
            g_LineAnnotationMaterial = Resources.Load("Materials/LineAnnotationColor") as Material;
            if (g_LineAnnotationMaterial == null)
            {
                Debug.LogError("Could not load Line Annotations Material");
            }
        }

        if (g_LineAnnotationsContainer == null)
        {
            g_LineAnnotationsContainer = GameObject.Find("LineAnnotationsContainer");
            if (g_LineAnnotationsContainer == null)
            {
                Debug.LogError("Could not load Line Annotations Container");
            }
        }

        if (g_IconAnnotationsContainer == null)
        {
            g_IconAnnotationsContainer = GameObject.Find("IconAnnotationsContainer");
            if (g_IconAnnotationsContainer == null)
            {
                Debug.LogError("Could not load Icon Annotations Container");
            }
        }
    }

    private void assetInitialization()
    {
        g_ButtonManager = this.GetComponent<ButtonClicking>();
        g_JsonManager = this.GetComponent<JSONManager>();
        g_SelectedElementTransform = null;
        g_AnnotationCounter = 0;
    }

    private void drawLine()
    {
        g_NewLine.GetComponent<LineRenderer>().positionCount = g_CurrentLine.Count;
        g_NewLine.GetComponent<LineRenderer>().SetPositions(g_CurrentLine.ToArray());
    }

    private void resetLineAnnotation()
    {
        g_CurrentLine = new List<Vector3>();
        g_NewLine = new GameObject();
        g_NewLine.name = "" + g_AnnotationCounter;
        g_NewLine.transform.SetParent(g_LineAnnotationsContainer.transform);
        g_NewLine.AddComponent<LineRenderer>();
        g_NewLine.GetComponent<LineRenderer>().material = g_LineAnnotationMaterial;
        g_NewLine.GetComponent<LineRenderer>().widthMultiplier = 10.0f; 
    }

    private void createPointAnnotation(Vector3 p_TouchedPoint3D)
    {
        float initial_point_distance = 5.0f;
        float angle;
        float val = Mathf.PI / 180.0f;

        int counter;

        //create enough points to make a round shape
        for (counter = 0; counter <= 360; counter = counter + 18)
        {
            //gets the new angle value
            angle = counter * val;

            //Calculates trigonometric values of the point
            float cosComponent = Mathf.Cos(angle) * initial_point_distance;
            float senComponent = Mathf.Sin(angle) * initial_point_distance;

            //Calculates the new point
            float transfX = ((cosComponent) - (senComponent));
            float transfY = ((senComponent) + (cosComponent));
            float transfZ = 0.0f; // we are representing the points in a 2D plane

            //assigns the results
            g_CurrentLine.Add(new Vector3((transfX) + (p_TouchedPoint3D.x), (transfY) + (p_TouchedPoint3D.y), (transfZ) + (p_TouchedPoint3D.z)));
        }
    }
    
    private void createIconAnnotation(Vector3 p_TouchedPoint3D, string p_ImageName)
    {
        GameObject NewImageContainer = new GameObject();
        NewImageContainer.AddComponent<Image>();
        NewImageContainer.GetComponent<Image>().name = "" + g_AnnotationCounter;
        NewImageContainer.GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/" + p_ImageName);
        NewImageContainer.transform.position = p_TouchedPoint3D;
        NewImageContainer.GetComponent<Image>().rectTransform.sizeDelta = new Vector2(200.0f, 200.0f);
        NewImageContainer.transform.localEulerAngles = new Vector3(0.0f, 0.0f, 45.0f);
        NewImageContainer.transform.SetParent(g_IconAnnotationsContainer.transform);

        // Stores the important info of the annotation for the JSON
        List<float> annotation_information = new List<float>();
        annotation_information.Add(p_TouchedPoint3D.x);
        annotation_information.Add(p_TouchedPoint3D.y);
        annotation_information.Add(NewImageContainer.transform.localEulerAngles.z);
        annotation_information.Add(NewImageContainer.transform.localScale.x);

        g_JsonManager.createJSONable(g_AnnotationCounter, "CreateAnnotationCommand", null, p_ImageName, annotation_information);
        g_AnnotationCounter++;
    }
}
