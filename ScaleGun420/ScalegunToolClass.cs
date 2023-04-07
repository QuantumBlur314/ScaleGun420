using HarmonyLib;
using JetBrains.Annotations;
using NAudio.MediaFoundation;
using OWML.Common;
using OWML.ModHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static ScaleGun420.ScaleGun420Modbehavior;  //What the cool kids are doin
using static ScaleGun420.StaffSpawner;

namespace ScaleGun420
{
    public class ScalegunToolClass : PlayerTool
    {
        private ObservableCollection<GameObject> _observableCollectionTest;  //subscribe to the CollectionChanged event.  Event's arguments are NotifyCollectionChangedArgs.  Might be helpful idk

        private bool _isLeavingEditMode = false;
        public bool _isInEditMode = false;
        private bool _isEditModeCentered = false;

        private Transform _camHoldTransform;
        private Transform _bodyHoldTransform;
        private Transform _bodyStowTransform;

        private DampedSpring3D _moveSpringPosition;

        private ScalegunPropClass _sgPropClass;
        private ScalegunAnimationSuite _animSuite;
        private SgComputer _toolComputer;
        private TheEditMode _toolEditMode;


        private void Awake()
        {
            LogGoob.WriteLine("ScalegunToolClass is woke, grabbing ScalegunPropClass, SgComputer, and TheEditMode...", MessageType.Success);
            //GetComponentInChildren doesn't search for inactive objects by default, needs to be set to (true) to find inactive stuff
            _sgPropClass = GetComponentInChildren<ScalegunPropClass>(true);  //Setting it to (true) worked ok fine idk whatever
            _toolComputer = GetComponentInChildren<SgComputer>();
            _toolEditMode = GetComponentInChildren<TheEditMode>();

            var _foundToolToStealTransformsFrom = Locator.GetPlayerBody().GetComponentInChildren<Signalscope>();  //
            if (_foundToolToStealTransformsFrom != null)
            {
                //WHY IS IT GOING TO ZERO LOCAL ROTATION ON INITIAL EQUIP WTF
                _holdTransform = _sgBodyHoldTransformGO.transform;
                _stowTransform = _foundToolToStealTransformsFrom._stowTransform;
                _moveSpring = new DampedSpringQuat(50, 8.5f, 1);  //032823: no more stuttering, I'm my own tool now  //040323_1737: Note that since _moveSpring isn't a static field, this only tweaks ScalegunTool's _moveSpring
                _moveSpringPosition = new DampedSpring3D(50, 8.5f, 1);

                _animSuite = this.gameObject.GetComponentInChildren<ScalegunAnimationSuite>();
            }
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
            _toolComputer.StopTheBabens();
            LeaveEditMode();
            base.UnequipTool();
            _toolComputer.ClearTerminal();
            this._sgPropClass.OnUnequipTool();
            if (_toolComputer.timerLoadingChildren != null || _toolComputer.siblingTimerCoroutine != null)
            { LogGoob.WriteLine("While Unequipping, one of the timers wasn't null.  Should probably _cancelTimer on those (coroutine breaks already reset value, but i never use _cancelTimer idfk))", MessageType.Error); }
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

        //Does injecting a field into a parameter only set the parameter's initial value, or does it check the field every time the parameter's used in the method?  If _selectedObject changes between when this coroutine starts and when the timer runs out, will it use the CURRENT _selectedObject, or will it have the value _selectedObject had when the coroutine started?
        //040523_1749: Corby confirms it's just like setting a var - it's a one-time copying of the field's value at that moment, and doesn't update.


        //What about other possible conditions of _selGO_Siblings than "null" or "equal to Child List"?  they're unaccounted for



        public override void Update()
        {
            base.Update();        //PlayerTool's base Update method handles deploy/stow anims; Everything else here is for Scalegun functions
            _animSuite.ToolPositionalUpdate(ref _isLeavingEditMode, ref _isEditModeCentered, ref _isInEditMode, 5f, _moveSpringPosition);                     // PlayerTool is already sketchy enough, I don't like the idea of locking modes behind hold positions idk, seems like a dangerous 

            if (!this._isEquipped || this._isPuttingAway)           //Only does additional stuff if ScalegunTool is equipped.  DISABLED ON A HUNCH  UPDATE HUNCH WAS WRONG, CARRY ON
            { return; }
            ///keeping timer in Update loop is "icky"
            //While-loops freeze the entire runtime until they're done, might not want that 
            //do the ol' TardisDematQueue "set it beyond the maximum time" trick
            if (_vanillaSwapper.IsInToolMode(SGToolmode) && OWInput.IsInputMode(InputMode.Character))
            {
                if (OWInput.IsNewlyPressed(InputLibrary.freeLook, InputMode.Character))  //Maybe don't restrict holding it up to your face, pointlessly raise the skill ceiling for dweebs who wanna practice blind-nav'ing Hierarchies because that's funny.
                {
                    if (!_isInEditMode)
                    {
                        _toolComputer.StopTheBabens();
                        EnterEditMode();
                    }
                    else { LeaveEditMode(); }
                }

                if (!_isInEditMode)
                {
                    if (OWInput.IsNewlyPressed(InputLibrary.toolActionPrimary))   //031823_1505: Changed a bunch of stuff to __instance for cleanliness; may or may not bork things //031823_1525: Okay so apparently that made it start nullreffing? //REBUILDING IS FAILING, THANKS MICROSOFT.NET FRAMEWORK BUG
                    {
                        _toolComputer.EyesDrillHoles();
                        Locator.GetPlayerBody().GetComponentInChildren<ProbeLauncherEffects>().PlayLaunchClip(false);
                    }

                    if (_toolComputer._selectedObject == null)
                    { return; }

                    if (ToParent)
                    { _toolComputer.OnToParent(); }
                    else if (ToChilds)  //Selected Object Text doesn't update for some reason?????????    //NEED LIST OF CHILDREN IN ORDER TO SCROLL FURTHER; Coroutines inevitable
                    { _toolComputer.OnToChilds(); }
                    //don't start a coroutine every damn time you press the button
                    else if (UpSibling)            //032223_1747: nullref???  //032323_1753: Setting the Bubbon bools static in Modbehavior lets me not need to do .Instance (with help from the Using: above) //040323_1423: learning this was a mistake
                    { _toolComputer.ToSiblingInDirection(1); }//STILL SCROLLS IN EditMode!!!! BAD!!!!
                    else if (DownSibling)
                    { _toolComputer.ToSiblingInDirection(-1); }
                }
                else if (_isInEditMode)
                {

                }
            }
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


