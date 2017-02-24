﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kinect = Windows.Kinect;

public class BodyView : MonoBehaviour
{

    public Material BoneMaterial;
    public GameObject BodyManager;
    public GameObject Camera;

    private Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong, GameObject>();
    private BodyManager _BodyManager;
    
    // map out all the bones by the two joints that they will be connected to
    private Dictionary<Kinect.JointType, Kinect.JointType> _BoneMap = new Dictionary<Kinect.JointType, Kinect.JointType>()
    {
        //Left leg
        { Kinect.JointType.FootLeft, Kinect.JointType.AnkleLeft },
        { Kinect.JointType.AnkleLeft, Kinect.JointType.KneeLeft },
        { Kinect.JointType.KneeLeft, Kinect.JointType.HipLeft },
        { Kinect.JointType.HipLeft, Kinect.JointType.SpineBase },

        //Right leg
        { Kinect.JointType.FootRight, Kinect.JointType.AnkleRight },
        { Kinect.JointType.AnkleRight, Kinect.JointType.KneeRight },
        { Kinect.JointType.KneeRight, Kinect.JointType.HipRight },
        { Kinect.JointType.HipRight, Kinect.JointType.SpineBase },

        //Left arm
        { Kinect.JointType.HandTipLeft, Kinect.JointType.HandLeft }, //Need this for HandSates
        { Kinect.JointType.ThumbLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.HandLeft, Kinect.JointType.WristLeft },
        { Kinect.JointType.WristLeft, Kinect.JointType.ElbowLeft },
        { Kinect.JointType.ElbowLeft, Kinect.JointType.ShoulderLeft },
        { Kinect.JointType.ShoulderLeft, Kinect.JointType.SpineShoulder },

        //Right arm
        { Kinect.JointType.HandTipRight, Kinect.JointType.HandRight }, //Needthis for Hand State
        { Kinect.JointType.ThumbRight, Kinect.JointType.HandRight },
        { Kinect.JointType.HandRight, Kinect.JointType.WristRight },
        { Kinect.JointType.WristRight, Kinect.JointType.ElbowRight },
        { Kinect.JointType.ElbowRight, Kinect.JointType.ShoulderRight },
        { Kinect.JointType.ShoulderRight, Kinect.JointType.SpineShoulder },

        //Spine and head
        { Kinect.JointType.SpineBase, Kinect.JointType.SpineMid },
        { Kinect.JointType.SpineMid, Kinect.JointType.SpineShoulder },
        { Kinect.JointType.SpineShoulder, Kinect.JointType.Neck },
        { Kinect.JointType.Neck, Kinect.JointType.Head },
    };
    // Update is called on each frames
    void Update()
    {
        //we need to store the keys of the tracked bodies in order to generate them
        //We check if the KinectManager has data to work with

        if (BodyManager == null)
        {
            return;
        }

        _BodyManager = BodyManager.GetComponent<BodyManager>();
        if (_BodyManager == null)
        {
            return;
        }
        //We store the data of the bodies detected
        Kinect.Body[] data = _BodyManager.GetData();
        if (data == null)
        {
            return;
        }

        /////////////////////////////////////////////////////////////////////////////////
        //List of the tracked bodies (ulong is a an unsigned integer)
        List<ulong> trackedIds = new List<ulong>();
        foreach (var body in data)
        {
            if (body == null)
            {
                continue;
            }

            if (body.IsTracked)
            {
                trackedIds.Add(body.TrackingId);  //We add the ID of all the tracked body from the the current frame in the tracked body list

                if (!_Bodies.ContainsKey(body.TrackingId))
                {
                    //if the body isn't already in the _Bodies dictionnary, we create a new body object and add it to the dictionnary
                    _Bodies[body.TrackingId] = CreateBodyObject(body.TrackingId, body);
                }
                //otherwise the body exists already and we have to refresh it
                RefreshBodyObject(body, _Bodies[body.TrackingId]);
            }
        }

        List<ulong> knownIds = new List<ulong>(_Bodies.Keys);

        // First delete untracked bodies
        foreach (ulong trackingId in knownIds)
        {
            if (!trackedIds.Contains(trackingId))   //we check every Ids in knownIds and check if the body are still tracked. If it isn't the case we destroy the body
            {                                       //by updating the _Bodies dictionnary
                Destroy(_Bodies[trackingId]);
                _Bodies.Remove(trackingId);
            }
        }
    }

    private GameObject CreateBodyObject(ulong id, Kinect.Body bodyKinect)
    {
        GameObject body = new GameObject("Body:" + id); //We create a new body object named by the id

        //Joint hierarchy flows from the center of the body to the extremities. It will go on each joint
        //The value of SpineBase = 0 and the value of the ThumbRight = 24, the maximum 
        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            GameObject jointObj = GameObject.CreatePrimitive(PrimitiveType.Sphere); //for each joint we create a gameObject with a sphere

            Kinect.Joint? targetJoint = null;

            if (_BoneMap.ContainsKey(jt))
            {
                targetJoint = bodyKinect.Joints[_BoneMap[jt]]; //parent of the analyzed joint
                //{ Kinect.JointType.FootLeft, Kinect.JointType.AnkleLeft }, AnkleLeft joint will be the targetJoint of FootLeft joint
            }

            if (targetJoint != null) //we add a cylinder only to the joints that have a parent 
            {
                GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cylinder.name = jt.ToString() + " bone";
                cylinder.transform.parent = jointObj.transform; //we attach the cylinder to the jointObj parent
            }

            jointObj.transform.localScale = new Vector3(1f, 1f, 1f);
            jointObj.name = jt.ToString();
            jointObj.transform.parent = body.transform; //we attach the joint to the body parent
        }

        return body;
    }

    private void RefreshBodyObject(Kinect.Body body, GameObject bodyObject)
    {
        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            Kinect.Joint sourceJoint = body.Joints[jt];
               //it will store the joint parent object of the current joint, if it doesn't have any parent -> = null
      
            Transform jointObj = bodyObject.transform.FindChild(jt.ToString()); //we store the position of the current jt
            jointObj.localPosition = GetVector3FromJoint(sourceJoint); //set the local position of each joint, we "scale" the distance of everything by 10
                                                                       //otherwise we would be represented as a heap of spheres

            //we habe to check if the current joint has another joint after
            if (_BoneMap.ContainsKey(jt))
            {
                Kinect.Joint targetJoint = body.Joints[_BoneMap[jt]]; //parent of the analyzed joint
                                                                      //{ Kinect.JointType.FootLeft, Kinect.JointType.AnkleLeft }, AnkleLeft joint will be the targetJoint of FootLeft joint
                Transform bone = jointObj.transform.FindChild(jt.ToString() + " bone");
                Transform targetJointObj = bodyObject.transform.FindChild(body.Joints[_BoneMap[jt]].ToString());
                bone.transform.position = new Vector3(jointObj.localPosition.x, jointObj.localPosition.y, jointObj.localPosition.z); //place the cylinder on their joint parent

                float Angle = Vector3.Angle(jointObj.eulerAngles, targetJointObj.eulerAngles);
                //bone.transform.eulerAngles = Angle;
                //Debug.Log(Angle);
                //bone.transform.LookAt(new Vector3(targetJoint.Value.Position.X, targetJoint.Value.Position.Y, targetJoint.Value.Position.Z));
                //bone.transform.position = new Vector3((sourceJoint.Position.X - targetJoint.Value.Position.X) * 10, (sourceJoint.Position.Y - targetJoint.Value.Position.Y) * 10, (sourceJoint.Position.Z - targetJoint.Value.Position.Z) * 10);
                //bone.Rotate(new Vector3(Angle, 0, 0));
            }

            //move the main camera on the head
            if (jt == Kinect.JointType.Head)
            {
                Camera.transform.position = new Vector3(jointObj.localPosition.x, jointObj.localPosition.y, jointObj.localPosition.z);
            }
        }
    }

    private static Vector3 GetVector3FromJoint(Kinect.Joint joint)
    {
        return new Vector3(joint.Position.X * 10, joint.Position.Y * 10, joint.Position.Z * 10);
    }

    private static Color GetColorForState(Kinect.TrackingState state)
    {
        switch (state)
        {
            case Kinect.TrackingState.Tracked:
                return Color.green;

            case Kinect.TrackingState.Inferred:
                return Color.red;

            default:
                return Color.black;
        }
    }
}
