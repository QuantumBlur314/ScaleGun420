using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


///MISSIONS: ADD EDITMODE


namespace ScaleGun420
{
    /// <summary>
    /// CURRENT OBJECTIVES:
    /// A.) holding down Right Click activates drag mode BY DEFAULT
    /// B.) Arrow/scroll/whatever changes it to other modes
    /// C.) maybe single right-click (not a full hold) is additional function
    /// </summary>
    public class TheEditMode : MonoBehaviour  //don't forget the 'U', so br*'ish
    {

        private List<LineRenderer> beamLines;
        private LineRenderer _beamsRenderer;

        private ScalegunToolClass _sEMTool;
        private ScalegunPropClass _sEMProp;
        private SgNavComputer _sEMCpu;

        private Coroutine dragObjectTimer;

        private GameObject _placeholderGO_SG;
        private GameObject _manipulatorGO_SG;
        private GameObject _theVictim;

        private Transform _playerCameraTransform;
        private Transform _originalVictimTransform;

        private PlayerLockOnTargeting _playerLockOnTargeting;

        private Vector3 _originalVictimPosition;
        private Quaternion _originalVictimRotation;

        private Vector3 _cursorPosition;
        private Quaternion _cursorRotation;

        private Quaternion _lockedCameraRotation;

        private Vector2 _cursorPos = Vector2.zero;
        private bool isInSubmode_SG;

        private bool _isDragging;
        private bool _isRotating;

        public float _lockonEasing = 0.5f;

        public float _originalWas45fIdk = 180f;







        public enum SG_Submode
        {
            Scalemode = 1,
            Spinmode = 2,
            Spawnmode = 3,
            Weldmode = 4,
        }

        private void Awake()
        {
            _sEMProp = Locator.GetPlayerBody().GetComponentInChildren<ScalegunPropClass>();
            _sEMCpu = Locator.GetPlayerBody().GetComponentInChildren<SgNavComputer>();
            _sEMTool = Locator.GetPlayerBody().GetComponentInChildren<ScalegunToolClass>();


            _placeholderGO_SG = GameObject.Find("Placeholder_SG");  //placeholder object can now be my designated funny friend, since _originalVictimTransform is what it is
            _manipulatorGO_SG = GameObject.Find("Cursor_SG");
            _playerCameraTransform = Locator.GetPlayerCamera().transform;

            if (Locator.GetPlayerTransform() != null)
            {
                _playerLockOnTargeting = Locator.GetPlayerTransform().GetRequiredComponent<PlayerLockOnTargeting>();
            }

            _placeholderGO_SG.SetActive(false);
            _manipulatorGO_SG.SetActive(false);
        
        }
        private void OnDisable()
        { }
        private GameObject GetSelectedObject()
        {
            if (_sEMCpu._selectedGOPublic == null)
                return null;
            return _sEMCpu._selectedGOPublic;
        }

        //should just be an OnEnable whatsit
        public void BeginEditing()  //Perhaps having the beans attached to a GO that gets disconnected from the thing
        {
            _placeholderGO_SG?.SetActive(true);
            _manipulatorGO_SG?.SetActive(true);
            _theVictim = GetSelectedObject();
            GOToLocalTransformOf(ref _placeholderGO_SG);  //this one is promble  
            GOToLocalTransformOf(ref _manipulatorGO_SG);  //Currently, this beast ends up wherever
            ///_manipulatorGO_SG.transform.parent = _playerCameraTransform; //041823_1354: //maybe don't do this?
        }

        private void GOToLocalTransformOf(ref GameObject phOrCursor)
        {
            if (_theVictim == null)
                return;
            phOrCursor.transform.parent = _theVictim.transform;   //why nullref   //this is prombaly bad idea in retrospect
            phOrCursor.transform.localPosition = new Vector3(0, 0, 0);
            phOrCursor.transform.localEulerAngles = new Vector3(0, 0, 0);
            phOrCursor.SetActive(true);
        }

        private void SetupPlaceholder()
        {
            GOToLocalTransformOf(ref _placeholderGO_SG);
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
                GOToLocalTransformOf(ref _manipulatorGO_SG);  //edit: nvm this is fine actually //THIS IS BAD, DO NOT DO THIS FOR THE MANIPULATOR, IT'S RUINING MY BEAMS, OR AT LEAST ADD A SEPARATE FREAK FOR COSMETIC //edit nvm lol xd
                _manipulatorGO_SG.transform.parent = Locator.GetPlayerCamera().transform;
                dragObjectTimer = StartCoroutine(VictimToCursorEvery(0.1f));
            }
        }

        private void FinalizeEdit()
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


        private void VictimToCursor()
        { }



        /// <summary>
        /// Nomai interface orbs follow the player's line of sight after a delay; try extrapolating that for moving victims.
        /// If you want rigidbody-type movement, you might want to reparent the victim to a makeshift rigidbody beast spawned as a sibling to the victim under the same parent, if possible.
        /// MAYBE MAKE A RIGIDBODY OVERRIDE WHOSE PHYSICS UPDATE AT LOWER FRAMERATE FOR PERFORMANCE'S SAKE
        /// IF YOU'RE GOING TO BE TURNING EVERY PARENTLESS VICTIM INTO A RIGIDBODY
        /// SNATCHING UP ORPHANS
        /// ENTOMBING THEM IN INVISIBLE ORBS
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
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
        private void RecenterCursor()
        { }

        private void BeginRotating()  //try locking onto a point directly offset from the player itself
        {

            //Vector3 ahead = Vector3.forward * 0.75f;
            //Vector3 worldPositionOfCamForward = _playerCameraTransform.TransformPoint(ahead);
            ///Vector3 relativeToPlaceholderGO = _placeholderGO_SG.transform.InverseTransformPoint(worldPositionOfCamForward);
            _cursorPos = new Vector2(0.5f, 0f);
            OWInput.ChangeInputMode(InputMode.Roasting);  //THIS SUCKS
            _isRotating = true;
            //_playerLockOnTargeting.LockOn(_placeholderGO_SG.transform, relativeToPlaceholderGO, _lockonEasing);
        }
        private void UpdateRotation(GameObject ofGuy)
        {
            Transform pivotTransform = ofGuy.transform;

            Vector2 axisValue = OWInput.GetAxisValue(InputLibrary.look, InputMode.Roasting);  //roasting mode sucks, can't use wasd or other controls.  also need map controls, and abandon ye lerps
            float num = (Mathf.Lerp(2f, 1f, 0.5f));
            this._cursorPos += axisValue * num * Time.deltaTime;
            if (this._cursorPos.magnitude > 1f)
            {
                this._cursorPos = this._cursorPos.normalized;
            }
            float num2 = this._cursorPos.magnitude * _originalWas45fIdk;
            Vector3 vector = Vector3.Cross(Vector3.forward, this._cursorPos);
            Quaternion quaternion = Quaternion.AngleAxis(num2, vector);
            pivotTransform.localRotation = Quaternion.Slerp(pivotTransform.localRotation, quaternion, 0.5f) * Quaternion.identity;
        }

        private void StopRotating()
        {
            if (!_isRotating)
                return;
            OWInput.ChangeInputMode(InputMode.Character);
            //_playerLockOnTargeting.BreakLock();
            _isRotating = false;
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

        private void Update()  //prevent this from running while in menu
        {
            if (!IsStaffInEditMode())  //oh this is actual hell ok
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
                    return;
                }
            }

            if (OWInput.IsPressed(InputLibrary.lockOn))
            {
                if (!_isRotating)
                    BeginRotating();
                else
                    UpdateRotation(_manipulatorGO_SG);
            }
            else if (_isRotating == true && !OWInput.IsPressed(InputLibrary.lockOn))
                StopRotating();


        }

    }
}
