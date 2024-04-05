
using System;
using UnityEngine;
using Unity.Collections;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARKit;
using Live2D.Cubism.Core;
using System.Runtime.InteropServices;
using Live2D.Cubism.Framework;
using Unity.VisualScripting;

public class FaceTracking : MonoBehaviour
{
    [SerializeField] private ARFaceManager faceManager;
    [SerializeField] private GameObject avatarPrefab;
    
    [SerializeField] private GameObject localAvatarPrefab;
    private CubismModel live2DModel;
    private CubismModel localLive2DModel;
    ARKitFaceSubsystem faceSubsystem;
    
    
    /// ローカルアバターのモーション更新パラメータ
    private CubismParameter faceAngleX;
    private CubismParameter faceAngleY;
    private CubismParameter faceAngleZ;
    private CubismParameter bodyAngleX;
    private CubismParameter bodyAngleY;
    private CubismParameter leftEye;
    private CubismParameter rightEye;
    private CubismParameter mouthForm;
    private CubismParameter mouthOpen;
    private CubismParameter eyeBallX;
    private CubismParameter eyeBallY;
    
    private float updateFaceAngleX;
    private float updateFaceAngleY;
    private float updateFaceAngleZ;
    private float updateLeftEye;
    private float updateRightEye;
    private float updateMouthForm;
    private float updateMouthOpen;
    
    
    
    /// リモートアバターのモーション更新パラメータ
    private CubismParameter localFaceAngleX;
    private CubismParameter localFaceAngleY;
    private CubismParameter localFaceAngleZ;
    private CubismParameter localLeftEye;
    private CubismParameter localRightEye;
    private CubismParameter localMouthForm;
    private CubismParameter localMouthOpen;
    private CubismParameter localEyeBallX;
    private CubismParameter localEyeBallY;
    
    private float _updateRemoteFaceAngleX;
    private float _updateRemoteFaceAngleY;
    private float _updateRemoteFaceAngleZ;
    private float _updateRemoteLeftEye;
    private float _updateRemoteRightEye;
    private float _updateRemoteMouthForm;
    private float _updateRemoteMouthOpen;
    
    
    [System.Serializable]
    public class  FaceTrackEntity
    { 
        public float faceAngleX; 
        public float faceAngleY;
        public float faceAngleZ;
        public float leftEye;
        public float rightEye;
        public float mouthForm;
        public float mouthOpen;
    }
    
    
    [DllImport("__Internal")]
    private static extern long updateFaceTrackingInfo(string message);

    private string jsonData;
    
    // LifeCycle

    private void Start()
    {
        Application.targetFrameRate = 60;
        live2DModel = avatarPrefab.GetComponent<CubismModel>();
        localLive2DModel = localAvatarPrefab.GetComponent<CubismModel>();
        
        // 表示したアバターとARKitを同期させる
        SetCubismParameter(live2DModel, localLive2DModel);
    }

    private void Update()
    {
        ConversionTrackingDataToJson();
        updateFaceTrackingInfo(jsonData);
    }

    private void OnEnable()
    {
        faceManager.facesChanged += OnFaceChanged;
    }

    private void OnDisable()
    {
        faceManager.facesChanged -= OnFaceChanged;
    }
    
    private void LateUpdate()
    {
        // ローカル表情
        localLeftEye.Value = updateLeftEye;
        localRightEye.Value = updateRightEye;
        localMouthForm.Value = updateMouthForm;
        localMouthOpen.Value = updateMouthOpen;
        
        //ローカル顔の向き
        localFaceAngleX.Value = updateFaceAngleX;
        localFaceAngleY.Value = updateFaceAngleY;
        localFaceAngleZ.Value = updateFaceAngleZ;
        
        // リモート表情
        leftEye.Value = _updateRemoteLeftEye;
        rightEye.Value = _updateRemoteRightEye;
        mouthForm.Value = _updateRemoteMouthForm;
        mouthOpen.Value = _updateRemoteMouthOpen;
        
        // リモート顔の向き
        faceAngleX.Value = _updateRemoteFaceAngleX;
        faceAngleY.Value = _updateRemoteFaceAngleY;
        faceAngleZ.Value = _updateRemoteFaceAngleZ;
    }
    
    private void OnFaceChanged(ARFacesChangedEventArgs eventArgs)
    {
        if (eventArgs.updated.Count != 0)
        {
            var arFace = eventArgs.updated[0];
            if (arFace.trackingState == TrackingState.Tracking
                && (ARSession.state > ARSessionState.Ready))
            {
                UpdateFaceTransform(arFace);
                UpdateBlendShape(arFace);
            }
        }
    }

    // Swiftから通話相手のFaceTrackingデータが送信されてくる
    // FaceTrackingデータのパースを行う関数
    public void receiveTrackingData(string inputDataString)
    {
        FaceTrackEntity entity = JsonUtility.FromJson<FaceTrackEntity>(inputDataString);
        _updateRemoteFaceAngleX = entity.faceAngleX;
        _updateRemoteFaceAngleY = entity.faceAngleY;
        _updateRemoteFaceAngleZ = entity.faceAngleZ;
        _updateRemoteLeftEye = entity.leftEye;
        _updateRemoteRightEye = entity.rightEye;
        _updateRemoteMouthForm = entity.mouthForm;
        _updateRemoteMouthOpen = entity.mouthOpen;
    }
    
    // Private 
    
    // 変数にアバターのモーションを定義する
    private void SetCubismParameter(CubismModel model, CubismModel localModel)
    {
        localFaceAngleX = localModel.Parameters[0];
        localFaceAngleY = localModel.Parameters[1];
        localFaceAngleZ = localModel.Parameters[2];
        localLeftEye = localModel.Parameters[3];
        localRightEye = localModel.Parameters[5];
        localMouthForm = localModel.Parameters[17];
        localMouthOpen = localModel.Parameters[18];
        
        faceAngleX = model.Parameters[0];
        faceAngleY = model.Parameters[1];
        faceAngleZ = model.Parameters[2];
        bodyAngleX = model.Parameters[22];
        bodyAngleY = model.Parameters[23];
        leftEye = model.Parameters[3];
        rightEye = model.Parameters[5];
        mouthForm = model.Parameters[17];
        mouthOpen = model.Parameters[18];
        
        
    }

    //顔の向きを更新する
    private void UpdateFaceTransform(ARFace arFace)
    {
        // 顔の位置データを取得
        Quaternion faceRotation = arFace.transform.rotation;
        
        float x = NormalizeAngle(faceRotation.eulerAngles.x)* 2f;
        float y = NormalizeAngle(faceRotation.eulerAngles.y);
        float z = NormalizeAngle(faceRotation.eulerAngles.z)* 2f;
        
    
        // 新しい顔の情報を変数にいれる
        updateFaceAngleX = y;
        updateFaceAngleY = x;
        updateFaceAngleZ = z;
    }
    
    // 表情を更新する
    private void UpdateBlendShape(ARFace arFace)
    {
        faceSubsystem = (ARKitFaceSubsystem)faceManager.subsystem;
        using var blendShapesARKit = faceSubsystem.GetBlendShapeCoefficients(arFace.trackableId, Allocator.Temp);
        foreach (var featureCoefficient in blendShapesARKit)
        {
            if (featureCoefficient.blendShapeLocation == ARKitBlendShapeLocation.EyeBlinkLeft)
            {
                updateLeftEye = 1 - featureCoefficient.coefficient;
            }
            if (featureCoefficient.blendShapeLocation == ARKitBlendShapeLocation.EyeBlinkRight)
            {
                updateRightEye = 1 - featureCoefficient.coefficient;
            }
            if (featureCoefficient.blendShapeLocation == ARKitBlendShapeLocation.MouthFunnel)
            {
                updateMouthForm = 1 - featureCoefficient.coefficient * 2;
            }
            if (featureCoefficient.blendShapeLocation == ARKitBlendShapeLocation.JawOpen)
            {
                updateMouthOpen = (float)(featureCoefficient.coefficient * 1.8);
            }
        }
    }
    
    // 顔の角度を正規化する
    private float NormalizeAngle(float angle)
    {
        if (angle > 180)
        {
            return angle - 360;
        }
        return angle;
    }

    // FaceTrackingで取得したデータをJsonに変換
    private void ConversionTrackingDataToJson()
    {
        FaceTrackEntity entity = new FaceTrackEntity();
        entity.faceAngleX = updateFaceAngleX;
        entity.faceAngleY = updateFaceAngleY;
        entity.faceAngleZ = updateFaceAngleZ;
        entity.leftEye = updateLeftEye;
        entity.rightEye = updateRightEye;
        entity.mouthForm = updateMouthForm;
        entity.mouthOpen = updateMouthOpen;

        jsonData = JsonUtility.ToJson(entity);
    }
    
}
