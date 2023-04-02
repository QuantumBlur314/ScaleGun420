using HarmonyLib;
using JetBrains.Annotations;
using NAudio.MediaFoundation;
using OWML.ModHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static ScaleGun420.ScaleGun420Modbehavior;  //What the cool kids are doin

namespace ScaleGun420
{
    //JUST COPY EVERYTHING OTHER TOOLS DO, EVEN IF THE METHODS ARE EMPTY.  CARGO CULT CODEBASE //Update: my power grows  //031923_1845: UPDATE: hubris
    public class ScalegunToolClass : PlayerTool
    {

        private ScalegunPropClass _sgPropClass;

        public static List<GameObject> _selGO_Siblings;
        public static List<GameObject> _selGO_Children;
        private bool _isLeavingEditMode = false;
        public bool _isInEditMode = false;
        private bool _isEditModeCentered = false;

        public Coroutine timer = null;
        private bool _cancelTimer = false;
        //private float _counter = 0;
        private float _timeLeft;

        private Transform _camHoldTransform;
        private Transform _bodyHoldTransform;
        private Transform _bodyStowTransform;
        public static GameObject _previousSelection;  //not used here, but ScalegunPropClass will use it to fill in adjacent UI fields without having to recalculate, //032323_1938: Actually this should probably be defined by the PropClass
        //private GameObject _parentOfSelection;
        public static GameObject _selectedObject;
        private GameObject _oldSelObject;  //not to be confused with _previousSelection
        public static GameObject _sgTrgt_Child;
        public static int _selObjIndex = 1;
        private DampedSpring3D _moveSpringPosition;


        private void Awake()
        {
            //GetComponentInChildren doesn't search for inactive objects by default, needs to be set to (true) to find inactive stuff
            _sgPropClass = GetComponentInChildren<ScalegunPropClass>(true);  //Setting it to (true) worked ok fine idk whatever

            var _foundToolToStealTransformsFrom = Locator.GetPlayerBody().GetComponentInChildren<Signalscope>();  //
            if (_foundToolToStealTransformsFrom != null)
            {

                //WHY IS IT GOING TO ZERO LOCAL ROTATION ON INITIAL EQUIP WTF
                _holdTransform = _sgBodyHoldTransformGO.transform;
                _stowTransform = _foundToolToStealTransformsFrom._stowTransform;
                _moveSpring = new DampedSpringQuat(50, 8.49f, 1);  //032823: no more stuttering, I'm my own tool now
                _moveSpringPosition = new DampedSpring3D(50, 8.5f, 1);
            }
            //new Vector3(0.5496f, -1.11f, -0.119f), new Vector3(343.8753f, 200.2473f, 345.2718f)  //local position and local euler angles
        }

        public override void Start()
        { base.Start(); } //disables tool by default, even Translator main. 

        public override void EquipTool()
        {
            base.EquipTool();
            if (this._isInEditMode)
            { }
            this._sgPropClass.OnEquipTool(); //Following in the footsteps of Translator/TranslatorPRop
        }

        public override void UnequipTool()          //CALLED BY ToolModeSwapper.EquipToolMode(ToolMode toolMode), which is itself called by ToolModeSwapper.Update
        {
            LeaveEditMode();
            base.UnequipTool();
            this._sgPropClass.OnUnequipTool();
            { }
            //base.UnequipTool SETS _isPuttingAway TO TRUE, THEN PlayerTool.Update APPLIES THE STOWTRANSFORMS THEN SETS base.enabled = false ONCE DONE ANIMATING
        }
        public void EnterEditMode()
        {
            if (!this._isInEditMode)
            {
                this._isInEditMode = true;
                this._isLeavingEditMode = false;
                this._isEditModeCentered = !this.HasEquipAnimation();

                this.transform.parent = _sgCamHoldTransformGO.transform;   //THE TOOL IS DUPLICATING (THUS DOUBLING) THE HoldTransformGO's TRANSFORM AS ITS _holdTransform.  THIS ISN'T OPTIMAL BUT YOU ALREADY ORIENTED IT IDFK WELL DONE I GUESS
                _holdTransform = _sgCamHoldTransformGO.transform;  //Oh wait, literally just don't make the transforms their parent, just make them a reference 

                // if (this.HasEquipAnimation())
                //{
                //    base.transform.localRotation = this._stowTransform.localRotation;
                // }
                //      base.enabled = true;
            }
        }

        public void LeaveEditMode()
        {
            if (_isInEditMode)
            {
                transform.parent = Locator.GetPlayerTransform();
                _holdTransform = _sgBodyHoldTransformGO.transform;
                this._isInEditMode = false;

                if (!this._isLeavingEditMode)
                {
                    this._isLeavingEditMode = true;
                    this._isEditModeCentered = false;
                }
            }
        }
        private enum PreviousSelectionToField
        {
            Parent = 0,
            Child = 1,
            SiblingAbove = 2,
            SiblingBelow = 3,
        }
        private enum DelayLoadingOf
        {
            Siblings = 0,
            Children = 1,
        }
        private IEnumerator WaitBeforeLoading(DelayLoadingOf familyMembers, float time)  //if multiple things call this, ensure each variant waits for other variants to finish to avoid chaos
        {
            _timeLeft = time;
            while (_timeLeft >= 0)
            {
                if (_cancelTimer)
                {
                    _cancelTimer = false;
                    break;
                }
                _timeLeft -= Time.deltaTime;
                yield return null;
            }
            //ensures input gets eaten this frame
            yield return new WaitForEndOfFrame();

            //do stuffs here 
            if (familyMembers == DelayLoadingOf.Siblings) //ADD A CONDITION FOR IF SIBLINGS LIST IS ALREADY ACTIVE FROM CHILD LIST
            {
                if (_selGO_Siblings == null)
                { _selGO_Siblings = _selectedObject.GetSiblings(); }
                else if (_selGO_Siblings == _selGO_Children)
                { _selGO_Children.Clear(); }
            }

            if (familyMembers == DelayLoadingOf.Children)
            {
                if (_selectedObject == _oldSelObject)  //if scrolling stopped on the same selection where it started, 
                {
                  //  _sgPropClass.UpdateScreenText();  //It's the same Selection, thus the _selGO_Children list (presumably) won't have changed; just refresh the screen
                    _oldSelObject = null;
                }
                else
                { _selGO_Children = _selectedObject.GetAllChildren(); }
            }  //If player loops back around and stops on same object, don't re-retrieve a new list of children.

            _sgPropClass.UpdateScreenText();

            timer = null;
        }

        public override void Update()
        {
            base.Update();        //PlayerTool's base Update method handles deploy/stow anims; Everything else here is for Scalegun functions

            float num = (this._isLeavingEditMode ? Time.unscaledDeltaTime : Time.deltaTime);
            Vector3 vector3 = (this._isLeavingEditMode ? this._stowTransform.localPosition : this._holdTransform.localPosition);
            base.transform.localPosition = _moveSpringPosition.Update(base.transform.localPosition, vector3, num);
            float num2 = Vector3.Angle(base.transform.localPosition, vector3);

            if (this._isInEditMode && !this._isEditModeCentered && num2 <= this._arrivalDegrees)
            { this._isEditModeCentered = true; }  //This might be causing trombles
            if (this._isLeavingEditMode && num2 <= this._arrivalDegrees)
            {
                this._isInEditMode = false;
                this._isLeavingEditMode = false;
                //base.enabled = false;
                this._moveSpringPosition.ResetVelocity();
            }

            if (!this._isEquipped || this._isPuttingAway)           //Only does additional stuff if ScalegunTool is equipped.  DISABLED ON A HUNCH  UPDATE HUNCH WAS WRONG, CARRY ON
            { return; }
            if (OWInput.IsNewlyPressed(InputLibrary.freeLook, InputMode.Character))  //Maybe don't restrict holding it up to your face, pointlessly raise the skill ceiling for dweebs who wanna practice blind-nav'ing Hierarchies because that's funny.
            {
                if (!_isInEditMode)
                { EnterEditMode(); }
                else { LeaveEditMode(); }
            }

            ///keeping timer in Update loop is "icky"

            //While-loops freeze the entire runtime until they're done, might not want that 

            //do the ol' TardisDematQueue "set it beyond the maximum time" trick


            if (_vanillaSwapper.IsInToolMode(SGToolmode) && OWInput.IsInputMode(InputMode.Character))
            {
                if (OWInput.IsNewlyPressed(InputLibrary.toolActionPrimary) && !_isInEditMode)   //031823_1505: Changed a bunch of stuff to __instance for cleanliness; may or may not bork things //031823_1525: Okay so apparently that made it start nullreffing? //REBUILDING IS FAILING, THANKS MICROSOFT.NET FRAMEWORK BUG
                { EyesDrillHoles(); }

                if (ToParent)
                {
                    if (_selectedObject == null || _selectedObject.transform.parent.transform.parent == null)  //prevents it from scrolling to final parent layer, as there's no way to find siblings at the highest level
                    { return; }
                    //_selGO_Siblings.Clear();
                    //PUT SOME EVENT HERE FOR THE PROP TO LISTEN FOR, MAKE AN EVENT FOR UPDATING THE SIBLING TEXT EVEN
                    _previousSelection = _selectedObject;
                    _selectedObject = _selectedObject.transform.parent.gameObject;
                    _sgPropClass.OnToParentInit();

                    if (timer == null)
                    { timer = StartCoroutine(WaitBeforeLoading(DelayLoadingOf.Siblings, 0.5f)); }
                    else
                    { _timeLeft += 0.2f; }
                }
                
                else if (ToChilds)
                {
                    if (_selectedObject == null || _selGO_Children == null)
                    { return; }
                    _previousSelection = _selectedObject;
                    _selGO_Siblings = _selGO_Children; //CAN I GO TO A CHILD, THEN DELAY THE LOADING OF ITS CHILDREN UNTIL I SCROLL TO THE DESIRED SPOT?!  CAN I SUSPEND THE TIMER IN LIMBO LIKE THAT?!
                                                       // _selGO_Children = null;  //DON'T DO THIS YET, WaitBeforeLoading CAN COMPARE _selGO_Children with _selGO_Siblings (a comparison probably more computationally intensive than it's worth tbh)
                    if (timer == null)
                    { timer = StartCoroutine(WaitBeforeLoading(DelayLoadingOf.Children, 0.5f)); }
                    else { LogGoob.WriteLine("This is the Else line at toolclass line 242, i couldn't visualize it"); }
                }
                //don't start a coroutine every damn time you press the button
                else if (UpSibling)            //032223_1747: nullref???  //032323_1753: Setting the Bubbon bools static in Modbehavior lets me not need to do .Instance (with help from the Using: above)
                {
                    if (_selGO_Siblings == null || _selGO_Siblings.Count <= 1)  //nvm this prevents scrolling before lists are loaded, already handled
                    { return; }

                    if (timer == null)
                    {
                        _oldSelObject = _selectedObject;
                        timer = StartCoroutine(WaitBeforeLoading(DelayLoadingOf.Children, 0.5f));
                    }
                    else
                    { _timeLeft += 0.2f; }
                    //THIS STUFF IS ABOVE THE SCROLLSIBLINGS STUFF BELOW ON A BLIND GAMBLE; CHANGE IF BORKEN

                    ScrollSiblings(1);
                    _sgPropClass.OnUpSiblings();
                }
                else if (DownSibling)
                {
                    if (_selGO_Siblings == null || _selGO_Siblings.Count <= 1)
                    { return; }

                    ScrollSiblings(-1);
                    _sgPropClass.OnDownSiblings();
                }

                //var siblingIndex = _selectedObject.transform.GetSiblingIndex();
                // var list = _selectedObject.transform.parent.GetChild(siblingIndex);
                // _sgPropClass.UpdateScreenText(_selectedObject, siblingIndex);
            }
        }

        public static GameObject GetSiblingAt(int increment = 1)
        {
            if (_selGO_Siblings.Count > 1)
            {
                var listLength = _selGO_Siblings.Count;
                var internalIndex = _selObjIndex;
                internalIndex += increment;
                internalIndex = ((internalIndex > listLength - 1) ? 0 : internalIndex);
                internalIndex = ((internalIndex < 0) ? listLength - 1 : internalIndex);
                var foundObject = _selGO_Siblings[internalIndex]; //these square brackets tell it to find the nth in the list, that's what these are
                return foundObject;
            }
            else { return _selectedObject; }
        }

        private void ScrollSiblings(int upOrDown)
        {
            _previousSelection = _selectedObject;
            var newSelection = GetSiblingAt(upOrDown);   //0323_1519: Idiot says this will always wrap around the list using "modulo" and Corby says to use .Count since .Count() will return Linq which is "stinky"
            _selObjIndex = newSelection.transform.GetSiblingIndex();
            _selectedObject = newSelection;
        }

        private void EyesDrillHoles()  //SHOULD EVENTUALLY IMPLEMENT WaitBeforeLoading TO THIS, TOO
        {
            Vector3 fwd = Locator.GetPlayerCamera().transform.forward;  //fwd is a Vector-3 that transforms forward relative to the playercamera
            Physics.Raycast(Locator.GetPlayerCamera().transform.position, fwd, out RaycastHit hit, 50000, OWLayerMask.physicalMask);

            var retrievedRootObject = hit.collider?.gameObject.transform.parent?.gameObject; //include condition for possibility that hit.collider has no parent somehow idk

            if (_selectedObject != null && retrievedRootObject == _selectedObject)
            { return; }
            _previousSelection = null;
            _selectedObject = retrievedRootObject;
            _selGO_Siblings = _selectedObject.GetSiblings();
            _selGO_Children = _selectedObject.GetAllChildren();
            _sgPropClass.UpdateScreenText();
        }

        public void ClearTerminal()
        {
            StopEditing();
            _selectedObject = null;
            //_parentOfSelection = null;
            _sgPropClass._sgpTxtGO_SibAbove = null;
            _sgPropClass._sgpTxtGO_SibBelow = null;
            _sgPropClass.UpdateScreenText();
        }

        public void StopEditing()
        { }



        // [[[  P R O P   S T U F F  ]]]

        private void OnEnable()
        {
            {
                if (!PlayerState.AtFlightConsole())        //borrowed from Signalscope.  Idk why different tool props have their OnEnable & OnDisable methods as different access levels
                {
                    _sgPropClass.enabled = true;  //TEST Ln114: disabled because NomaiTranslator doesn't override this at all //update: THIS ISN'T AN OVERRIDE, PlayerTool DOESN'T HAVE AN OnEnable BY DEFAULT// 031823_0637: disabling since it wasn't in the holy scriptures of NomaiTranslator, idfk anymore// 032123_1917: Disabling because NomaiTranslatorProp doesn't even define a gameobject.
                }
            }
        }
        private void OnDisable() //RUNS _sgPropClass.OnFinishUnequipAnimation, which runs _sgOwnPropGroupject.SetActive(false)
        {
            _sgPropClass.OnFinishUnequipAnimation();//031623_2054: Error?  nullref with set_enabled?  _sgPropClass undefined?
        }





    }
}


