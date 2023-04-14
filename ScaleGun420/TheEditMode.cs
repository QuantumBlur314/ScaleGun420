using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ScaleGun420
{
    public class TheEditMode : MonoBehaviour  //don't forget the 'U', so br*'ish
    {

        private List<LineRenderer> beamLines;
        private LineRenderer _beamsRenderer;

        private ScalegunToolClass _sEMTool;
        private ScalegunPropClass _sEMProp;
        private SgComputer _sEMCpu;

        private Coroutine dragObjectTimer;

        private GameObject _placeholderGO_SG;
        private GameObject _manipulatorGO_SG;
        private GameObject _theVictim;

        private Transform _playerCameraTransform;

        private bool isInSubmode_SG;

        private bool _isDragging;


        private void Awake()
        {
            _sEMProp = Locator.GetPlayerBody().GetComponentInChildren<ScalegunPropClass>();
            _sEMCpu = Locator.GetPlayerBody().GetComponentInChildren<SgComputer>();
            _sEMTool = Locator.GetPlayerBody().GetComponentInChildren<ScalegunToolClass>();
            _placeholderGO_SG = GameObject.Find("Placeholder_SG");
            _manipulatorGO_SG = GameObject.Find("Cursor_SG");
            _playerCameraTransform = Locator.GetPlayerCamera().transform;

            _placeholderGO_SG.SetActive(false);
            _manipulatorGO_SG.SetActive(false);
        }
        private GameObject GetSelectedObject()
        {
            if (_sEMCpu._selectedGOPublic == null)
                return null;
            return _sEMCpu._selectedGOPublic;
        }
        public void BeginEditing()
        {
            _placeholderGO_SG?.SetActive(true);
            _manipulatorGO_SG?.SetActive(true);
            _theVictim = GetSelectedObject();
            ObjectToVictim(ref _placeholderGO_SG);  //this one is promble  
            ObjectToVictim(ref _manipulatorGO_SG);
            _manipulatorGO_SG.transform.parent = _playerCameraTransform;
        }
        private void ResetFrameGOs()
        { }


        private void StartDraggin()
        {
            if (dragObjectTimer != null)
                return;
            else
            {
                ObjectToVictim(ref _placeholderGO_SG);
                ObjectToVictim(ref _manipulatorGO_SG);
                _manipulatorGO_SG.transform.parent = Locator.GetPlayerCamera().transform;
                dragObjectTimer = StartCoroutine(VictimToCursorEvery(0.1f));
            }
        }

        private void FinalizeDraggin()
        { }
        private void CancelCurrentEdit()
        { LosslessPositionalTransformEXPERIMENTAL(_theVictim, _placeholderGO_SG); }

        private void LosslessPositionalTransformEXPERIMENTAL(GameObject objectToMove, GameObject destinationGO)
        {
            var movinTFInternal = objectToMove.transform;
            var destinationTFInternal = destinationGO.transform;
            movinTFInternal.position = destinationTFInternal.position;
            movinTFInternal.rotation = destinationTFInternal.rotation;
        }

        private void ObjectToVictim(ref GameObject phOrCursor)
        {
            if (_theVictim == null)
                return;
            phOrCursor.transform.parent = _theVictim.transform;   //why nullref
            phOrCursor.transform.localPosition = new Vector3(0, 0, 0);
            phOrCursor.transform.localEulerAngles = new Vector3(0, 0, 0);
            phOrCursor.SetActive(true);
        }
        private void VictimToCursor()
        { }

        public enum SG_Submode
        {
            Scalemode = 1,
            Spinmode = 2,
            Spawnmode = 3,
            Weldmode = 4,
        }
        private IEnumerator VictimToCursorEvery(float time)
        {
            for (; ; )
            {
                if (_placeholderGO_SG == null || _isDragging == false)
                {
                    yield break;
                }
                else
                    LosslessPositionalTransformEXPERIMENTAL(_theVictim, _manipulatorGO_SG);
                yield return new WaitForSeconds(time);
            }
        }
        public bool AllowSwangin()
        {
            return this.IsStaffInEditMode() && _sEMTool._isEditModeCentered && !_sEMTool._isLeavingEditMode;
        }
        ///private void Update()
        /// {
        ///  if (this._shipController != null && this.AllowSwangin() && OWInput.IsNewlyReleased(InputLibrary.freeLook, InputMode.All))
        ///  {
        ///      this.CenterCameraOverSeconds(0.33f, true);  //Check ShipCockpitController for reference, 
        ///   }
        ///    if (OWTime.IsPaused(OWTime.PauseType.Reading))
        ///    {
        ///        this.UpdateCamera(Time.unscaledDeltaTime);
        ///     }
        /// <summary>
        /// private void Update()
        /// </summary>
        /// 

        bool IsStaffInEditMode()
        { return _sEMTool._isInEditMode; }

        private void Update()
        {
            if (!IsStaffInEditMode())
                return;
            if (!_isDragging)
            {
                if (OWInput.IsPressed(InputLibrary.probeRetrieve, 0.5f))
                {
                    _sEMCpu._probeLauncherEffects.PlayRetrievalClip();
                    _isDragging = true;
                    StartDraggin();
                }
                else
                {

                }
            }
            else if (_isDragging)
            {
                if (OWInput.IsNewlyReleased(InputLibrary.probeRetrieve))
                {
                    _isDragging = false;
                    dragObjectTimer = null;
                    //FinalizeDraggin();
                }
                else if (!IsStaffInEditMode())
                {
                    _isDragging = false;
                    CancelCurrentEdit();
                }
            }

            if (_isDragging && _sEMTool._isLeavingEditMode)
            {
                _isDragging = false;
                CancelCurrentEdit();
            }
            else
                return;

        }

    }
}
