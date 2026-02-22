using UnityEngine;
using System;
using System.Collections.Generic;
using static Define;
using Data;
using System.Collections;

public static class NullServices
{
    public sealed class NullInputActionMapController : IInputActionMapController, INullServiceProxy<IInputActionMapController>
    {
        public static readonly NullInputActionMapController Instance = new();
        private NullInputActionMapController() { }

        private Action<string> _OnActionMapChanged;

        public event Action<string> OnActionMapChanged
        {
            add    => _OnActionMapChanged += value;
            remove => _OnActionMapChanged -= value;
        }

        public string CurrentActionMapGroup => "";
        public Nullable<E_InputActionMap> CurrentActionMapType => default(Nullable<E_InputActionMap>);

        public void PushActionMapGroup(E_InputActionMap mapType)
        {
        }

        public void PushActionMapGroup(string groupName)
        {
        }

        public void PopActionMapGroup()
        {
        }
        public void TransferTo(IInputActionMapController real)
        {
            if (_OnActionMapChanged != null) real.OnActionMapChanged += _OnActionMapChanged;
            _OnActionMapChanged = null;
        }

    }


    public sealed class NullCoroutineRunner : ICoroutineRunner, INullServiceProxy<ICoroutineRunner>
    {
        public static readonly NullCoroutineRunner Instance = new();
        private NullCoroutineRunner() { }


        public Coroutine Run(IEnumerator routine) => null;

        public void Stop(Coroutine coroutine)
        {
        }
        public void TransferTo(ICoroutineRunner real) { }

    }


    public sealed class NullBuildingCardUI : IBuildingCardUI, INullServiceProxy<IBuildingCardUI>
    {
        public static readonly NullBuildingCardUI Instance = new();
        private NullBuildingCardUI() { }


        public void AddCard(GameEntity addUnit, Vector3 worldPosition, bool isInit)
        {
        }

        public void RestoreSaveDatas(IEnumerable<BaseData> datas)
        {
        }

        public List<BaseData> CaptureSaveData() => new System.Collections.Generic.List<BaseData>();
        public void TransferTo(IBuildingCardUI real) { }

    }


    public sealed class NullCameraInfoProvider : ICameraInfoProvider, INullServiceProxy<ICameraInfoProvider>
    {
        public static readonly NullCameraInfoProvider Instance = new();
        private NullCameraInfoProvider() { }

        public Vector3 Position => Vector3.zero;
        public Quaternion Rotation => Quaternion.identity;

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
        }
        public void TransferTo(ICameraInfoProvider real) { }

    }


    public sealed class NullCameraRig : ICameraRig, INullServiceProxy<ICameraRig>
    {
        public static readonly NullCameraRig Instance = new();
        private NullCameraRig() { }

        private Action<int> _OnChangeLookFloor;

        public event Action<int> OnChangeLookFloor
        {
            add    => _OnChangeLookFloor += value;
            remove => _OnChangeLookFloor -= value;
        }

        public int CurrentLookFloor => default(int);

        public float GetCameraHeight() => default(float);
        public void TransferTo(ICameraRig real)
        {
            if (_OnChangeLookFloor != null) real.OnChangeLookFloor += _OnChangeLookFloor;
            _OnChangeLookFloor = null;
        }

    }


    public sealed class NullCameraShakeSettings : ICameraShakeSettings, INullServiceProxy<ICameraShakeSettings>
    {
        public static readonly NullCameraShakeSettings Instance = new();
        private NullCameraShakeSettings() { }


        public void SetImpulseReactionDuration(float duration)
        {
        }
        public void TransferTo(ICameraShakeSettings real) { }

    }


    public sealed class NullCameraInput : ICameraInput, INullServiceProxy<ICameraInput>
    {
        public static readonly NullCameraInput Instance = new();
        private NullCameraInput() { }


        public Vector2 GetCameraMoveVector() => default(Vector2);
        public void TransferTo(ICameraInput real) { }

    }


    public sealed class NullInputQuery : IInputQuery, INullServiceProxy<IInputQuery>
    {
        public static readonly NullInputQuery Instance = new();
        private NullInputQuery() { }

        public bool IsRightClick => default(bool);
        public void TransferTo(IInputQuery real) { }

    }


}
