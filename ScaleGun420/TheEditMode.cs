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
        private Transform _originalVictimTransform;


        private Vector3 _originalVictimPosition;
        private Quaternion _originalVictimRotation;


        private bool isInSubmode_SG;

        private bool _isDragging;


        private void Awake()
        {
            _sEMProp = Locator.GetPlayerBody().GetComponentInChildren<ScalegunPropClass>();
            _sEMCpu = Locator.GetPlayerBody().GetComponentInChildren<SgComputer>();
            _sEMTool = Locator.GetPlayerBody().GetComponentInChildren<ScalegunToolClass>();
            _placeholderGO_SG = GameObject.Find("Placeholder_SG");  //placeholder object can now be my designated funny friend, since _originalVictimTransform is what it is
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
        public void BeginEditing()  //Perhaps having the beans attached to a GO that gets disconnected from the thing
        {
            _placeholderGO_SG?.SetActive(true);
            _manipulatorGO_SG?.SetActive(true);
            _theVictim = GetSelectedObject();
            ObjectToVictimTransform(ref _placeholderGO_SG);  //this one is promble  
            ObjectToVictimTransform(ref _manipulatorGO_SG);
            _manipulatorGO_SG.transform.parent = _playerCameraTransform;
        }
        private void ResetFrameGOs()
        { }
        private void SetupPlaceholder()
        {
            ObjectToVictimTransform(ref _placeholderGO_SG);
            if (_originalVictimTransform.parent != null)
            {
                _placeholderGO_SG.transform.parent = _originalVictimTransform.parent;
            }
            else _placeholderGO_SG.transform.parent = null;
        }


        private void StartDraggin()
        {
            if (dragObjectTimer != null)
                return;
            else
            {
                _originalVictimTransform = _theVictim.transform;
                SetupPlaceholder();
                ObjectToVictimTransform(ref _manipulatorGO_SG);  //edit: nvm this is fine actually //THIS IS BAD, DO NOT DO THIS FOR THE MANIPULATOR, IT'S RUINING MY BEAMS, OR AT LEAST ADD A SEPARATE FREAK FOR COSMETIC //edit nvm lol xd
                _manipulatorGO_SG.transform.parent = Locator.GetPlayerCamera().transform;
                dragObjectTimer = StartCoroutine(VictimToCursorEvery(0.1f));
            }
        }

        private void FinalizeDraggin()
        { }

        //if i somehow eventually get to the point of fully reparenting victims, probably best to hop em back to 
        private void CancelCurrentEdit()
        {
            dragObjectTimer = null;
            LosslessPositionalTransformEXPERIMENTAL(_theVictim, _placeholderGO_SG.transform);
        }

        private void LosslessPositionalTransformEXPERIMENTAL(GameObject objectToMove, Transform destinationTransform) //A TRANSFORM ON ITS OWN IS LIKE AN ADDRESS, THE THING CAN MOVE AND ITS TRANSFORM WILL STILL TARGET IT
        {
            var movinTFInternal = objectToMove.transform;

            movinTFInternal.position = destinationTransform.position;
            movinTFInternal.rotation = destinationTransform.rotation;
        }

        private void ObjectToVictimTransform(ref GameObject phOrCursor)
        {
            if (_theVictim == null)
                return;
            phOrCursor.transform.parent = _theVictim.transform;   //why nullref   //this is prombaly bad idea in retrospect
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
                    LosslessPositionalTransformEXPERIMENTAL(_theVictim, _manipulatorGO_SG.transform);
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

        /// <summary>
        /// Although the dragging after unequipping the tool is currently a bug, I could probably do something with it, i.e. let user switch to rotation mode while dragging
        /// </summary>

        private void Update()
        {
            if (!IsStaffInEditMode())
            {
                if (_isDragging)
                {
                    CancelCurrentEdit();
                    _isDragging = false;
                }
                return;
            }
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
            }

            else
                return;

        }
        private void OnDisable()
        { }

    }
}
