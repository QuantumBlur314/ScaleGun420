using Newtonsoft.Json.Serialization;
using OWML.Common;
using System;
using System.CodeDom;
using System.Threading.Tasks;
using System.Text;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ScaleGun420.Extensions;  //had to do this for some bs reason idfk
using XGamingRuntime;

namespace ScaleGun420
{

    //041023_1557: SUCCESSFULlY made _selectedGOPublic only update upon completion of processes, using internal fields where necessary instead
    /// <summary>
    /// MISSION UPDATE: Needlessly complex, but perhaps prevent going down paths that exclusively contain colliders?
    /// </summary>
    public class SgNavComputer : MonoBehaviour
    {
        private ScalegunPropClass _sgPropClass;
        private ScalegunToolClass _sgToolClass;

        private List<GameObject> _siblingsOfSelGO;
        private List<GameObject> _childGOList;
        ///public static ObservableCollection<GameObject> _observableCollectionTest;  //subscribe to the CollectionChanged event.  Event's arguments are NotifyCollectionChangedArgs.  Might be helpful idk

        private bool _interruptBabenCycle;
        private bool _babenCycleShouldRun = false;
        private Coroutine timerBabyCycle = null;
        private float _timeBeforeStartCycling;

        private bool _timerStartedByNavToChild = false;
        private bool _loadingKids_OnToAdjSibsBehalf = false;

        public Coroutine timerChildrenLoading = null;
        public Coroutine timerSiblingsLoading = null;

        public bool _cancelLoadChildren = false;
        public bool _cancelLoadSiblings = false;

        private bool _babensToggleOn = false;

        private int _hasScrolledToParentXTimes = 0;

        private float _timeBeforeChildrenLoad;
        private float _timeBeforeSiblingsLoad;


        //public static GameObject _previousSelection;  //not used here, but ScalegunPropClass will use it to fill in adjacent UI fields without having to recalculate, //032323_1938: Actually this should probably be defined by the PropClass
        //private GameObject _parentOfSelection;

        public GameObject _selectedGOPublic;

        private GameObject _currentSelInternal_onToChild;
        private GameObject _finalSelection_OnToChild;
        private GameObject _freshSelInternal_OnToParent;

        private GameObject _candidateInternal_onToChilds;   //THIS ISN'T GETTING UPDATED

        public int _selecIndex = 0;
        private int _displayedChildIndex = 0;

        private float _coroutineTimerStartValueUniv = 0.5f;
        private float _subsequentPressIncrementUniv = 0.25f;
        public ProbeLauncherEffects _probeLauncherEffects;

        public static string _colliderFilter = "Collider";
        public List<string> _forbiddenObjects = new List<string>() { "Collider", "Collision" };


        //CLASS IS ENABLING LATE, ONLY ON EQUIP; ALSO, CONSIDER DEACTIVATING COMPUTER WHILE TOOL IS UNEQUIPPED
        private void Awake()
        {
            LogGoob.WriteLine("SgNavComputer is woke, grabbing ScalegunPropClass...", MessageType.Success);
            _sgToolClass = Locator.GetPlayerBody().GetComponentInChildren<ScalegunToolClass>();
            _sgPropClass = Locator.GetPlayerBody().GetComponentInChildren<ScalegunPropClass>();
            _probeLauncherEffects = Locator.GetPlayerBody().GetComponentInChildren<ProbeLauncherEffects>();
        }
        private void Start()
        { base.enabled = false; }

        public void OnEquipTool()
        { enabled = true; }

        public void OnUnequipTool()
        {
            StopCyclingChildren();  //this probably can't run/the rest of UnequipTool can't finish until _toolComputer is active
            _sgToolClass.LeaveEditMode();
            ClearTerminal();
            KillNavCoroutines();
            enabled = false;
        }
        private void AAADebug_SetNewTimerValues(float timerStartValues = 1f, float subsequentPressValues = 0.5f)
        {
            _coroutineTimerStartValueUniv = timerStartValues;
            _subsequentPressIncrementUniv = subsequentPressValues;
        }

        public enum IndexMarkerState
        {
            ToParentSecondScroll = -69,
        }
        private enum CoroutineStartedBy
        {

        }
        private enum DelayLoadingOf
        {
            Siblings = 0,
            Children = 1,
        }

        public bool CanEnterEditMode()
        {
            if (_selectedGOPublic == null)
            {
                LogGoob.WriteLine("SgNavComputer.CanEnterEditMode ~100: Was called when _selectedObjectPublic was null; you shouldn't be doing that anywhere, but currently happening on startup", MessageType.Warning);
                return false;
            }
            if (_selectedGOPublic.ToString().Contains(_colliderFilter))
            {
                LogGoob.Scream("Inadvisable to edit colliders");
                return false;
            }
            return true;
        }

        private void SetPubSelGOFieldAs(GameObject setToThis, bool updateSelGOIndexToo = true)
        {
            _selectedGOPublic = setToThis;
            if (updateSelGOIndexToo)
                SetSelGOIndexVia(_selectedGOPublic);
        }
        private void SetSelGOIndexVia(GameObject thisGO)
        { _selecIndex = thisGO.transform.GetSiblingIndex(); }

        private void FlushChildListPlusIndex(bool resetChildIndex = false)
        {
            _childGOList = null;
            if (resetChildIndex)
                _displayedChildIndex = 0;
        }
        private void FlushNavListsAndIndices()
        {
            FlushChildListPlusIndex(true);
            _siblingsOfSelGO = null;
            _selecIndex = 0;
        }

        private bool AreSiblingsLoaded()
        { return _siblingsOfSelGO != null; }

        private bool DoesChildListExist()
        { return _childGOList != null; }

        private bool ShouldChildListExist()
        {
            return !(_childGOList != null && _hasScrolledToParentXTimes >= 2);
        }

        public GameObject GetPubSelection()
        { return _selectedGOPublic; }

        /// <summary>
        /// The override here sucks tbh, not intuitive enough
        /// </summary>
        /// <param name="optionalObjectToCompare"></param>
        /// <param name="setSelGOPub"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public GameObject GetGOAtSelecIndexCheck(GameObject optionalObjectToCompare = null, bool setSelGOPub = false)  //_siblingsOfSelGO starts null  
        {
            GameObject foundObject = _selecIndex.FindIndexedGOIn(_siblingsOfSelGO);
            if (optionalObjectToCompare == null || foundObject == optionalObjectToCompare)
            {
                if (setSelGOPub == true)
                    SetSelGOIndexVia(foundObject);
                return foundObject;
            }
            throw new Exception($"SelectionAtSiblingIndex Comparror: foundObject {foundObject} and optionalObjectToCompare {optionalObjectToCompare} weren't the same");
        }

        //Currently starts running on Up or DownSiblings if _timeLeftChildren isn't already running



        // if (timesScrolledToSiblings <= 0 && staleSelectionThisPress == null)
        // originalGOThisPress = GetPubSelection();
        // else if (timesScrolledToSiblings >= 1 && staleSelectionThisPress != null)
        //  originalGOThisPress = GetPubSelection();
        //  else
        // throw new Exception($"OnToParent 145ish: Unaccounted-for scrollstate, setting originalGOThisPress from GetPubSelection().  {_hasScrolledToParentXTimes}, {staleSelectionThisPress}");
        //now no longer dumb, but very messy  //now no longer messy

        //you should probably nullcheck this you dingus

        /// <summary>
        /// LEAVES A TRAIL OF CHILD INDEX
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void NavToParent()   //see what happens if I let it run while children are loading.  I suspect it will break hellishly but whatever
        {
            if (timerChildrenLoading != null)
            {
                LogGoob.Scream("sorry, loading; try it now");
                return;
            }
            GameObject staleSelectionThisPress = _selectedGOPublic; ///_freshSelInternal_OnToParent;  //where does this get set?  //screw this
            Transform originalParentTransformThisPress = staleSelectionThisPress.transform.parent;  //maybe just an extension that slaps another .transform.parent.gameObject to the end of the previous, for however many times you press Up?  sounds easier

            int timesScrolledToParent = _hasScrolledToParentXTimes;

            if (originalParentTransformThisPress != null)
            {
                GameObject originalParentThisPress = originalParentTransformThisPress.gameObject;
                Transform firstGrandparentTransform = originalParentTransformThisPress.parent;
                string upperSibling;
                string lowerSibling;

                DelayBabenCycle();

                if (timesScrolledToParent <= 0 && timerSiblingsLoading == null && AreSiblingsLoaded() == true)
                {
                    CopySiblingsToChildList();   //The aforementioned flush lets us push the current siblings into the child field without issue,

                    if (timesScrolledToParent == 0)
                    {
                        timesScrolledToParent = 1;
                        _hasScrolledToParentXTimes = timesScrolledToParent;
                        timerSiblingsLoading = StartCoroutine(LoadSiblingsAfter(_coroutineTimerStartValueUniv));  //handles the rest, retrieves fresh _selectedObject index if unavailable; also can probably use _selectObject unless u wanna fuckin... inject it as a parameter
                        LogGoob.WriteLine("NavToParent: Scrolled first time", MessageType.Info);
                    }
                    else throw new Exception($"NavToParent: _hasScrolledToParentXTimes already at {_hasScrolledToParentXTimes} on first scroll??");

                    if (firstGrandparentTransform != null)
                    {
                        upperSibling = "Siblings waiting"; lowerSibling = "Siblings waiting";
                    }
                    else
                    { upperSibling = "limit"; lowerSibling = "limit"; }
                    //loadsiblingsafter already should handle null parent just fine
                }
                else if (timesScrolledToParent >= 1 && timerSiblingsLoading != null)
                {
                    _timeBeforeSiblingsLoad += _subsequentPressIncrementUniv;
                    timesScrolledToParent += 1;
                    _hasScrolledToParentXTimes = timesScrolledToParent;
                    FlushChildListPlusIndex();
                    _siblingsOfSelGO = null;

                    if (firstGrandparentTransform != null)
                    { upperSibling = "Scroll resuming,"; lowerSibling = "Cleaned caches"; }
                    else
                    { upperSibling = "Reached Firmament,"; lowerSibling = "Sisters nonviable "; }
                    LogGoob.WriteLine("NavToParent: Scrolled second time", MessageType.Info);
                }
                else throw new Exception($"NavToParent: unaccounted-for Safe scroll conditions, idk,  scrolled {_hasScrolledToParentXTimes} times, had {_timeBeforeSiblingsLoad} seconds left, and the coroutine is {timerSiblingsLoading}.  AreSiblingsLoaded is {AreSiblingsLoaded()}");

                //_displayedChildIndex = 0;  //shouldn't this always be the index of the old selected?  why am i setting it to 0 here?
                int staleSelIndex = _selecIndex;
                int actualStaleSelIndex = staleSelectionThisPress.transform.GetSiblingIndex();
                if (staleSelIndex != actualStaleSelIndex)
                {
                    _selecIndex = actualStaleSelIndex;
                    throw new Exception("NavToParent: _selecIndex doesn't match current _selectedGOPublic sibling index!!! set to correct one for next press but wtf, should've set it right elsewhere");
                }
                SetPubSelGOFieldAs(originalParentThisPress, true);
                _displayedChildIndex = actualStaleSelIndex;

                //And now we can tell it to select the one we were on, possibly needless but if Babens start Cycling somehow, it'll be happy

                string newParentTxtCandidate = "firmament.ax15";
                //string newSelection = $"{newSelectionAfterPress}";  //nullcheck already exists 
                string newChild = $"{staleSelectionThisPress}";  //should never be null
                if (firstGrandparentTransform != null)
                    newParentTxtCandidate = GOToStringOrElse(firstGrandparentTransform.gameObject, "wtf");

                _sgPropClass.RefreshScreen(newParentTxtCandidate, upperSibling, lowerSibling, newChild);
            }
            else
            {
                LogGoob.Scream("CAN'T BREACH FIRMAMENT", MessageType.Info);
                return;
            }
        }

        //Does injecting a field into a parameter only set the parameter's initial value, or does it check the field every time the parameter's used in the method?  If _selectedObject changes between when this coroutine starts and when the timer runs out, will it use the CURRENT _selectedObject, or will it have the value _selectedObject had when the coroutine started?
        //040523_1749: Corby confirms it's just like setting a var - it's a one-time copying of the field's value at that moment, and doesn't update.
        /// <summary>
        /// EXCLUSIVELY used by NavToParent
        /// Update: NEEDS TO ACCOUNT FOR ABSENCE OF GOD
        /// Update: to be more specific, needs to account for possibility that user has scrolled all the way to Firmament and Siblings cannot be accessed.
        /// maybe load children instead?  at this point all optimization is out the fuckin window.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>

        //CONFIRMED THAT THIS IS SETTING THE UPPER AND LOWER SIBLINGS VISUALLY SWAPPED UNTIL THE FIRST SUBSEQUENT SIBLINGSCROLL
        private IEnumerator LoadSiblingsAfter(float time)  //if multiple things call this, ensure each variant waits for other variants to finish to avoid chaos
        {
            _timeBeforeSiblingsLoad = time;  //the start of a coroutine only happens once, confirmed
            while (_timeBeforeSiblingsLoad >= 0)
            {
                if (_cancelLoadSiblings)
                {
                    _cancelLoadSiblings = false;
                    LogGoob.WriteLine("LoadSiblingsAfter successfully canceled by _cancelLoadSiblings", MessageType.Message);
                    yield break;
                }
                _timeBeforeSiblingsLoad -= Time.deltaTime;
                yield return null;
            }
            //ensures input gets eaten this frame
            yield return new WaitForEndOfFrame();

            GameObject selectionNow = _selectedGOPublic;
            int timesScrolledToParents = _hasScrolledToParentXTimes;
            int newSelIndex = selectionNow.transform.GetSiblingIndex(); //NavToChild: doesn't happen.      NavToParent: empty, needs refresh.      NavToSibling: doesn't happen without Siblings to Nav(you're looking at the coroutine that loads the siblings) 
                                                                        // List<GameObject> currentChildsList = _childGOList;
            List<GameObject> newSiblingsList = selectionNow.GetAllSiblings();

            newSelIndex.StringAdjacentSiblingsElse(newSiblingsList, out string upperSibling, out string lowerSibling);
            _sgPropClass.RefreshScreen("SKIP", upperSibling, lowerSibling, "SKIP");  //oh yeah no this is backwards im stupid af

            if (ShouldChildListExist() == false)  //NavToParent: first scroll unnecessary, second scroll needs, _childGOList should be ACTUALLY, NavToParent should ALWAYS generate a childlist; NavToChild, on the other hand, doesn't't scroll to siblings until it's done loadingsiblings here.  
                LogGoob.WriteLine("LoadSiblingsAfter ~325: scrolled to parent >= 2 times, _childGOList should've already been null but wasn't .  Next line sets it fresh from _selGO anyway, but address nonetheless; CycleBabens may be to blame", MessageType.Warning);


            if (timesScrolledToParents >= 2)  //should be clear
                _childGOList = selectionNow.ListChildrenOrNull();
            _hasScrolledToParentXTimes = 0;
            _selecIndex = newSelIndex;
            _siblingsOfSelGO = newSiblingsList;

            StopCoroutine(timerSiblingsLoading);   //THIS PART OF THE FUNCTION ENDS NORMAL //you can put stuff after this, nothing here is stopping the current code from running.  just ensures it doesn't happen again once it hits the end.
            timerSiblingsLoading = null;
        }
        //SetPubSelGOFieldAs(selectionNow, false);  //this shouldn't be necessary anymore; //this is literally being set at the beginning of the finishing youches of this coroutine - coroutines, the thing that forbids anything else from running until they either finish or yield
        //yield break;    //NOT yield break, the thing's already ending



        /// <summary>
        /// EVERY PRESS SETS _selObjPublic?  YEP
        /// 
        /// OnToChilds CAN ONLY RUN ONCE PER LoadChildsAfter() ACTIVATION
        /// 
        /// Either OnToChilds OR NavToSibling can start the LoadChildsAfter() timerChildrenLoading coroutine,
        /// but ONLY NavToSibling can happen again while that's running.
        /// 
        /// Pressing OnToChilds, then quickly using NavToSibling, can extend the time before a new child list is loaded,
        /// saving the creation of the new list of children, until the user's settled on the sibling to generate the childlist FROM,
        /// A WHOPPING ONE(1) List<> less than a normal, sane system would've had to generate.
        /// This is a negligible microoptimization.
        /// The whole idea behind this class is negligible microoptimizations.
        /// If you're not a fan of very small red-and-blue Peterbilts then get out of my goddamn zone
        /// 
        /// NavToSibling NEVER CLEARS _childGOList, SINCE _timerLoadingChildren SAVES THE LIST IN CASE USER SCROLLS TO SAME SPOT
        /// but if OnToChilds has been run at all, it probably shouldn't do any of that since guh

        /// 
        /// NavToParent probably shouldn't run then either, ohhhh
        /// </summary>
        // When NavToChild has been run, what conditions does it change?
        // it can nullify the childlist, that's a good signal.  not great for code readability tho
        //maybe a flag or a flog
        //or perchance a dog

        //UNDER CONSTRUCTION: true


        public void NavToChild() //Add condition for scrolling to very bottom of the well
        {
            //AN UPDATE:   USING THE LoadChildrenAfter COROUTINE IS NEEDLESSLY SLOW AND HAS TO RUN A COROUTINE EVERY TIME; this should have its own coroutine to wait until done scrolling down before it loads ALL OTHER HIERARCHIES i guess
            //Wait, why would you want to scroll indiscriminately
            if (_childGOList == null || timerChildrenLoading != null || _timerStartedByNavToChild || _selectedGOPublic == null)  //removed "_childGOList == null" check, since we want to scroll to the bottom//Can't gatekeep whether timerChildrenLoading's running here, or else it will just skip the whole function
                return;
            DelayBabenCycle(0.75f);

            //wait, we WANT it to gatekeep the whole function.  If the timerChildrenLoading is running, it means the Children are Loading (i.e. not ready to be scrolled to)
            List<GameObject> childListAtPress = _childGOList;
            int kidIndexAtPress = _displayedChildIndex;

            GameObject newCandidateFromChild = kidIndexAtPress.FindIndexedGOIn(childListAtPress) ?? throw new System.Exception("OnToChilds: couldn't find GO @ _childIndexCurrent for any number of reasons.  check logs i guess.");
            GameObject priorSelectionGO = _selectedGOPublic;
            //_vvv  an update  vvv_ You don't update selecIndex or sibling list until a few lines down, why are you even using GetGOAtSelecIndexCheck here????
            //upon pressing NavToChild after selecting slate(parentless), apparently newCandidateFromChild was correct, but the index wasn't                    //foundObject was the same as before the press, didn't yield the child somehow
            timerChildrenLoading = StartCoroutine(LoadChildrenAfter(_coroutineTimerStartValueUniv));
            _timerStartedByNavToChild = true;

            //_candidateInternal_onToChilds = newCandidateFromChild; //wtf is this  //
            _siblingsOfSelGO = childListAtPress;
            _selecIndex = kidIndexAtPress;

            FlushChildListPlusIndex(true);
            SetPubSelGOFieldAs(newCandidateFromChild); //PENDING

            string siblingAbove = "";
            string siblingBelow = "";
            if (newCandidateFromChild.transform.parent.childCount > 1)
                newCandidateFromChild.TextFromAdjacentSiblingsIn(childListAtPress,
                    out siblingAbove,
                    out siblingBelow, "SKIP");


            ///why must this bastard so insistently nullref
            _sgPropClass.RefreshScreen(
                $"{priorSelectionGO}, prevsel",
                siblingAbove,
                siblingBelow,
                "pending update...",  // ____ .   .   .   Y   O   U   .   .   .____  ...are probably being overwritten by the stuff in LoadChildrenAfter
                $"{newCandidateFromChild}");
        }

        //_hasScrolledToParentXTimes = 1;
        //Coroutine saves the CURRENT _selectedObject to _oldSelObject; when _timerChildren runs out, will check whether _selectedObject is same as _old.  I think that means scrolling up and down with impunity wiil be fine, but again, sort the other garbage first




        //SetPubSelGOFieldAs(_currentSelInternal_onToChild);  //OnToChild SHOULDN'T EVEN START IF NO CHILD EXISTs;DO I NEED THIS INTERNAL VALUE FOR NavToSibling?!

        //WHAT DO YOU MEAN GetChild() BY INDEX ALREADY EXISTS
        //Really wanna make _selectedObject local but The Brain Wall, The Bricks



        //_displayedChildIndex = 0;  //this shafts an insignificant cosmetic feature in LoadChildrenAfter; is it worth it? //YES, BURN
        ///_selGO_Children = _selectedGOPublic.GetChildListElseNull();  //this ruins the whole "delay loading of new child list until after coroutine" plan, but whatever
        // var proposedChildList = _childGOList;  ///children are gone wgat are you doing
        //string nextChildText = " d"; //this will not be updated here if coroutine works as planned
        // if (proposedChildList != null)
        //    nextChildText = $"{proposedChildList[0]}";  //FindIndexedGOIn isn't necessary



        //if scrolled multiple times, then shouldn't've scrolled multiple times, wtf is wrong with you.

        //CAN I GO TO A CHILD, THEN DELAY THE LOADING OF ITS CHILDREN UNTIL I SCROLL TO THE DESIRED SPOT?!  CAN I SUSPEND THE TIMER IN LIMBO LIKE THAT?!  //040223_2006: More trouble than it's worth for now, maybe later.  Just deal with that list being generated and pray it doesn't lag.
        // _childGOList = null;  //DON'T DO THIS YET, WaitBeforeLoading CAN COMPARE _childGOList with _siblingsOfSelGO (a comparison probably more computationally intensive than it's worth tbh)
        //if (timer == null)                      
        //{ timer = StartCoroutine(WaitBeforeLoading(DelayLoadingOf.Children, 0.5f)); }
        // else { LogGoob.WriteLine("ToolClass Ln236: ToChilds: Timer wasn't null; didn't start WaitBeforeLoading of Children, or do anything else"); }


        //for some reason, this never terminates because its time somehow stops above 0 and it stays in limbo
        //THROWN EXCEPTIONS CAUSE ANY METHOD, POSSIBLY INCLUDING COROUTINES, TO STOP IN THEIR TRACKS.  THAT MIGHT BE WHY THE COUNTER'S GETTING STUCK ABOVE ZERO
        /// <summary>
        /// Not directly responsible for actually handling Current Selected objects, LoadChildrenAfter is only concerned with the delayed
        /// loading of the children of whatever object the user settles upon in their browsing.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="selectionWhenTimerStarted"></param>
        /// <returns></returns>
        private IEnumerator LoadChildrenAfter(float time, GameObject selectionWhenTimerStarted = null)  //if multiple things call this, ensure each variant waits for other variants to finish to avoid chaos
        {
            _timeBeforeChildrenLoad = time;   //WHY IN GOD'S NAME IS _timeLeftChildren STUCK ABOVE 0, THIS ISN'T JUST BECAUSE I DON'T _cancelTimerChildren ON UNEQUIP, IT'S HAPPENING WHILE EQUIPPED
            while (_timeBeforeChildrenLoad >= 0)
            {
                if (_cancelLoadChildren)   //_cancelTimerChildren will stop the timer; remember to clear any values 
                {
                    _cancelLoadChildren = false;  //probably maybe cancel this with navigations that might conflict?  idk
                    timerChildrenLoading = null;
                    break;
                }
                _timeBeforeChildrenLoad -= Time.deltaTime;
                yield return null;
            }
            //ensures input gets eaten this frame
            yield return new WaitForEndOfFrame();


            GameObject currentSelection = _selectedGOPublic;  //this consensus thing is stupid, die
            List<GameObject> currentChildList = _childGOList;  //this defeats the whole purpose of the process but who care, now i guess it's just a failsafe
            int currentChildIndex = _displayedChildIndex;  //BECAUSE OF THE WAY CHILDREN ARE DISPLAYED & LOADED, I'M IN HELL
            int proposedChildIndex;  //ToChild already handles this; does ToSiblings? 
            List<GameObject> proposedChildList;  //this is an emergency thing idfk

            bool navToChildStartedThisTimer = _timerStartedByNavToChild;
            bool shouldPullFreshChildList = (navToChildStartedThisTimer || (selectionWhenTimerStarted != currentSelection));  //Corny busted out some sort of Punnet Square for if-genetics and proved this is the inverse of the flag3 so im good
                                                                                                                              // bool flag3 = (!_timerStartedByNavToChild && newSelSameAsOld);
            if (!shouldPullFreshChildList)
            {
                proposedChildList = currentChildList;
                proposedChildIndex = currentChildIndex;
                //this was originally meant to check whether the INDEXES matched.  somehow i ended up writing this around GOs instead, and including a local variable meant to represent a list on accident to boot.  well done.
            }   // proposedChildIndex = currentChildIndex;  //If NavToChild has run at all, this will be 0.  if BabyCycle was erroneously running during coroutine, it may be some number beyond the index (babentimer has always been bad)
            else if (shouldPullFreshChildList)  //<--- if this, then don't bother checking whether you're where you started; being back where you started is theoretically impossible
            {
                proposedChildList = currentSelection.ListChildrenOrNull();  //this will return null if no children exist.
                proposedChildIndex = 0;
                if (navToChildStartedThisTimer && currentChildList != null)
                    LogGoob.WriteLine("LoadChildrenAfter ~500: despite NavToChild beginning this coroutine, currentChildList wasn't null.  NavToChild method should be setting childlist null by end of method", MessageType.Warning);
            }                                                            //OnToSiblings REQUIRES an initial value to start at, unless you want different conditions for a post-OnToChild scroll
            else
                throw new Exception($"Wackditions.  _childGOList is {currentChildList}. also this means Corby was wrong and flag3 wasn't a perfect inverse of shouldPullFreshChildList");  //nullref

            if (proposedChildIndex > proposedChildList.Count)
                throw new Exception("New proposed child index was above the prosposed list.count, reset newChildIndex to 0 as failsafe");


            if (shouldPullFreshChildList)  //the stuff in "else if (!shouldPullFreshChildList)" above is just for clarity's sake, the whole point of the method is not having to update these fields unless conditions render the current fields outdated
            {
                GameObject firstChildAtNewChildIndex = null;
                if (proposedChildList != null)
                    firstChildAtNewChildIndex = proposedChildIndex.FindIndexedGOIn(proposedChildList);

                _childGOList = proposedChildList; //currentSelection.ListChildrenOrNull() WILL RETURN NULL IF NO CHILDREN EXIST //internalConsensusList_LCA was null  //did it again  //and again
                _displayedChildIndex = proposedChildIndex;  //proposedChildIndex exists specifically so _arbitraryChildIndex can be defined outside the brackets //ACTUALLY, ToCHILD SHOULD PROBABLY LEAVE THIS INDEX IN A MARKER STATE AND OH BOY HERE I GO LOOPING

                _sgPropClass.RefreshScreen("SKIP", "SKIP", "SKIP", GOToStringOrElse(firstChildAtNewChildIndex, "Maximum depth?"), "SKIP"); //042023_2208: Nice.  Getting "Children not found" despite the presence of children, and subsequently being able to scroll down to said children. //040623_1222: _childGOList[proposedChildIndex] got an OutOfRangeException from something //Another nullref from scrolling up fast, seems to make subsequent vertical scrolls no longer update the child list
            }
            else LogGoob.WriteLine($"Back where we started, shouldn't have to load new child list", MessageType.Success);          //SHOULD PROBABLY MAKE UNEQUIPPING CANCEL ALL CURRENT COROUTINES //NavToChild should go all the way down to the bottom of the well, genius. //a nullref.  also this is all lagging to hell  //another nullref    //anotha one

            _timerStartedByNavToChild = false;
            StopCoroutine(timerChildrenLoading);
            timerChildrenLoading = null;
        }
        //LogGoob.WriteLine($"LoadChildrenAfter ~465: set _childGOList to {_childGOList} & _displayedChildIndex to {_displayedChildIndex}");


        //This has to run after either DelayLoadingOf.Children condition, so                          


        ///StopCoroutineStartBabies(ref timerChildrenPending);  //can confirm this corouttine wasn't stopping while the other one was, so i did this here too.

        //selectionWhenTimerStarted shouldn't be null, because OnToChild shouldn't be able to run again if it started this timerChildrenLoading, and I think NavToParent should be forbidden
        //GRAB WHICHEVER OBJECT THE OnToChild AND NavToSibling METHODS LAST SETTLED ON INTERNALLY (_toChildsToSibsInternalConsensusGO)
        //GameObject internalConsensusList_LCA = _candidateInternal_onToChilds;
        //CHECK IF IT WAS THE SAME AS THE ORIGINAL SELECTION (AUTOMATICALLY RULE OUT POSSIBILITY IF OnToChild WAS USED SUCCESSFULLY)

        //if (_currentSelInternal_onToChild == null)  //coroutine SHOULDN'T BE RUNNING
        // yield break;
        //pull it in



        //IF  SAME AS ORIGINAL SELECTION, DO NOTHING (MAYBE REFRESH CHILD TEXT)
        //ELSE, SET FINALIZED IN STONE


        //GET NEW CHILD LIST (Won't even get here if selection was same, remember "if(){ if(optionsal){continue;} skippablestuff;} continuestuff;" exists)
        //IF CHILDLIST DOES NOT EXIST, COMMUNICATE THIS THROUGH CHILD TEXT FIELD


        //IF OnToChild WAS RUN, SIBLING LIST WILL EXIST (formerly child list, which may or may not exist anymore)
        //IF OnToChild WAS RUN, NO NEED TO GET SIBLING LIST
        //NEVER NEED TO GET SIBLING LIST ACTUALLY NVM




        /// <summary>
        /// The below is NOT finishing its job, _childGOList isn't getting updated by scrolling up and down
        /// 
        /// 
        /// FIRST SIBLING SCROLL GOES THE WRONG DIRECTION, OR MAYBE DISPLAY AIN'T SHOWING RIGHT, IDFK
        /// 
        /// </summary>
        /// AFTER A SINGLE SIBLING SCROLL, NavToParent BREAKS.  WHY

        public void NavToSibling(int direction = 1)   //could probably microOptimize by splitting it up again and having different conditions using some weird hidden tags depending on whether a field was generated fresh or from prevSel, but no fuck you
        {
            List<GameObject> listOfSiblings = _siblingsOfSelGO;

            if (timerSiblingsLoading != null || listOfSiblings == null || listOfSiblings.Count <= 1)
                return;
            DelayBabenCycle();

            GameObject priorSelection = _selectedGOPublic;
            int oldNewPriorIndex = priorSelection.transform.GetSiblingIndex();
            int oldSmellySelecIndex = _selecIndex;
            if (oldSmellySelecIndex != oldNewPriorIndex)
            {
                _selecIndex = oldNewPriorIndex;
                LogGoob.Scream("NavToSibling ~590: _selectedGOPublic's SiblingIndex didn't match current stored _selecIndex, set it to match but watch it.", MessageType.Error);
            }
            //MAKE ADDITIONAL CONDITIONS FOR WHEN THE LIST IS ONLY 2
            GameObject coolNewSelection = oldNewPriorIndex.AdjacentSiblingIn(listOfSiblings, direction);
            GameObject upcomingSibling = coolNewSelection.AdjacentSiblingOfGOIn(listOfSiblings, direction);             //_selectedObject = brandSelectionGO;  //disabled on a hunch aka mercy

            SetPubSelGOFieldAs(coolNewSelection, true);

            string newSelectionText = GOToStringOrElse(coolNewSelection, "SKIP");

            if (timerChildrenLoading == null)
                timerChildrenLoading = StartCoroutine(LoadChildrenAfter(_coroutineTimerStartValueUniv, priorSelection));  //Coroutine saves the CURRENT _selectedObject to _oldSelObject; when _timerChildren runs out, will check whether _selectedObject is same as _old.  I think that means scrolling up and down with impunity wiil be fine, but again, sort the other garbage first
            else
                _timeBeforeChildrenLoad += _subsequentPressIncrementUniv;

            string upperSiblingTxt = "SKIP";  //why am i doing it with strings here instead of objects, why am i like this
            string lowerSiblingTxt = "SKIP";
            if (direction > 0)
            {
                upperSiblingTxt = $"{upcomingSibling}";
                lowerSiblingTxt = $"{priorSelection}, prev";
            }
            else if (direction < 0)
            {
                upperSiblingTxt = $"{priorSelection}, prev";
                lowerSiblingTxt = $"{upcomingSibling}";
            }
            _sgPropClass.RefreshScreen("SKIP", upperSiblingTxt, lowerSiblingTxt, "SKIP", newSelectionText);  //Refresh screen already pulls from _selectedGOPublic
        }



        private void StartBabyCheck()
        {
            if (timerBabyCycle != null)
                return;
            else
                timerBabyCycle = StartCoroutine(CycleBabens(1));                          //starts cycling through babies
        }

        private void FixedUpdate()  //this seems to run once at awake 
        {
            if (this.enabled && _selectedGOPublic != null && !AreNavCoroutinesRunning())
                StartBabyCheck();
        }

        private void KillNavCoroutines()
        {
            if (timerSiblingsLoading != null)
            {
                LogGoob.WriteLine($"KillNavCoroutines ~635: timerSiblingsLoading was running", MessageType.Message);
                _cancelLoadSiblings = true;
            }
            if (timerChildrenLoading != null)
            {
                LogGoob.WriteLine("KillNavCoroutines !635: timerChildrenLoading was running", MessageType.Message);
                _cancelLoadChildren = true;
            }
        }
        public bool AreNavCoroutinesRunning()
        { return (timerSiblingsLoading != null || timerChildrenLoading != null); }

        public void StopCyclingChildren()
        {
            if (_babenCycleShouldRun == true)
                _babenCycleShouldRun = false;
            if (timerBabyCycle != null)
            {
                if (_babenCycleShouldRun == true)
                {
                    LogGoob.WriteLine("StopCyclingChildren ~670: _babenCycleShouldRun was true, so set it to false.");
                    _babenCycleShouldRun = false;    //this will set _shouldBabens to false and deactivate the timingBabens coroutine when in edit mode
                }
                StopCoroutine(timerBabyCycle);  //routine is null?
                timerBabyCycle = null;
            }
            else
                LogGoob.WriteLine("StopCyclingChildren ~675: timerBabyCycle was already null.  Don't worry about it", MessageType.Info);
            if (_babenCycleShouldRun == true)
            {
                _babenCycleShouldRun = false;
                throw new Exception("StopCyclingChildren Incongruity: despite timerBabyCycle already being null, _babenCycleShouldRun was somehow true.");
            }
        }

        private void DelayBabenCycle(float baseDelayTime = 0.75f, float timerToAddTo = 0f, string childFieldText = "SKIP")
        {
            _timeBeforeStartCycling += (baseDelayTime + timerToAddTo);
            _babenCycleShouldRun = false;
            _sgPropClass.RefreshScreen("SKIP", "SKIP", "SKIP", childFieldText, "SKIP");
        }

        // INSTEAD OF RUNNING ENDLESSLY AND RISKING INPUTS OVERLAPPING THE BABENCYCLE,
        // MAYBE SAVE BUTTONPRESSES IN SOME KIND OF INPUT QUEUE
        //THAT DOESN'T GET READ UNTIL THE CURRENT BABEN LOOP IS DONE???
        //and the next baben loop doesn't happen until the input queue's been read/all related coroutines are done

        //maybe also just put the checks inside this

        /// <summary>
        /// CHANGING THE "yield return waitForSeconds" TO "continue" MAKES GAME FREEZE
        /// </summary>
        /// <param name="upDown"></param>
        /// <param name="time"></param>
        /// <param name="cycleInterruptTime"></param>
        /// <param name="newChildText"></param>
        /// <returns></returns>

        private IEnumerator CycleBabens(int upDown, float time = 1f, float cycleInterruptTime = -4f, string newChildText = "")  //should probably make sure other children exist first
        {
            WaitForSeconds waitForSeconds = new(time);
            for (; ; )
            {
                if (_selectedGOPublic == null || _sgToolClass._isInEditMode || AreNavCoroutinesRunning())
                {
                    yield return new WaitForSeconds(time);
                    continue;
                }

                if (!_babenCycleShouldRun)
                {
                    if (_timeBeforeStartCycling >= 0)
                    {
                        _timeBeforeStartCycling -= Time.deltaTime;   //WHY IN GOD'S NAME IS _timeLeftChildren STUCK ABOVE 0, THIS ISN'T JUST BECAUSE I DON'T _cancelTimerChildren ON UNEQUIP, IT'S HAPPENING WHILE EQUIPPED
                        yield return null;
                    }
                    yield return new WaitForEndOfFrame();
                    _babenCycleShouldRun = true;
                }

                var freshChildList = _childGOList;  //do i even need this if I'm no longer doing the thing
                if (freshChildList == null)
                {
                    GameObject freshDad = _selectedGOPublic;

                    if (freshDad.transform.childCount > 0)  //a nullref? but why tho  //WHY ARE YOU WHY WHY WHY
                    {
                        freshChildList = freshDad.ListChildrenOrNull();
                        _childGOList = freshChildList;  //INCLUDE METHODS ELSEWHERE IN CycleBabens TO ENSURE THE CHILD INDEX EXISTS
                        LogGoob.Scream("CycleBabens ~730: Despite _selectedGOPublic's nonzero childCount, _childGOList was null; generating emergency childlist", MessageType.Error);  //This complains when the Cursor gets added to an object's hierarchy when EditMode activates, don't ask why CycleBabens is running while EditMode is active because idk //ok so this just threw
                    }
                    else yield return waitForSeconds;
                    continue;
                }

                int staleChildIndex = _displayedChildIndex;
                int currentChildCount = freshChildList.Count;  //this == freshDad.transform.childCount, isn't this redundant?//nullref
                GameObject nominatedChild = null;
                if (staleChildIndex <= currentChildCount)
                {
                    if (currentChildCount > 1)
                        nominatedChild = staleChildIndex.AdjacentSiblingIn(freshChildList, 1);
                    else
                    {
                        yield return waitForSeconds;
                        continue;
                    }
                }
                else
                {
                    int failsafeIndex = 0;
                    staleChildIndex = failsafeIndex;
                    nominatedChild = failsafeIndex.FindIndexedGOIn(freshChildList);
                    LogGoob.WriteLine("CycleBabens ~710: _displayedChildIndex was beyond _childGOList count; set to failsafe index 0; Fix this", MessageType.Error);
                }
                //maybe make lists a whole class component to attach to current index somehow?  idk if that's possible 

                _displayedChildIndex = nominatedChild.transform.GetSiblingIndex();  //the above converts an index to a GO, then this line converts it back from a GO to an index.  hell hell nightmare nightmare scream scream
                newChildText = $"{nominatedChild}";

                _sgPropClass.RefreshScreen("SKIP", "SKIP", "SKIP", newChildText, "SKIP");

                yield return waitForSeconds;
                continue;
            }

        }

        //_sgPropClass.RefreshScreen("SKIP", "SKIP", "SKIP", newChildText, "SKIP");




        /// <summary>
        /// Raycasts until collider, gets that's attached gameobject, plugs it all into the computer
        /// </summary>
        public void EyesDrillHoles()  //SHOULD EVENTUALLY IMPLEMENT WaitBeforeLoading TO THIS, TOO
        {
            Vector3 fwd = Locator.GetPlayerCamera().transform.forward;  //fwd is a Vector-3 that transforms forward relative to the playercamera
            Physics.Raycast(Locator.GetPlayerCamera().transform.position, fwd, out RaycastHit hit, 50000, OWLayerMask.physicalMask);
            if (hit.collider == null)
            { return; }
            else
            {
                GameObject currentSelection = _selectedGOPublic; //built-in nullcheck //never mind
                GameObject newPickedObject = hit.collider.gameObject;  //why do i have to nullcheck this too ffs //oh it's literally just when you aim at empty space ok
                Transform parentTransformOfNew = newPickedObject.transform.parent;
                if (parentTransformOfNew != null)  //can't check whether parentTransformOfNew.gameObject is null, if parentTransformOfNew ITSELF is already null, thus error.  
                {
                    newPickedObject = parentTransformOfNew.gameObject;
                    parentTransformOfNew = parentTransformOfNew.parent;
                }

                //vvv___vvv  IS _selectedObject NULL ALMOST EVER??????  vvv___vvv
                if (newPickedObject == currentSelection)  //probably make this internal, idk , trying to phase out _selectedObject in favor of index, but idk
                    return;

                //LogGoob.WriteLine($"EyesDrillHoles ~670: newPickedObject is {GOToStringOrElse(newPickedObject, "NULL AUUUUGH")}");
                RefreshSGSelection(newPickedObject);     //this en
                _probeLauncherEffects.PlayLaunchClip(false);
            }
            //a new instance of this starts running fresh every time the staff fires; you were warned about this exact element of coroutines earlier yet here you are making the mistake again - surely by the second time you'd have adequate experience to know better!!!! smh fr, fr!!!!
        }
        private void RefreshSGSelection(GameObject objectToInternalSelection, string parentField = "", string siblingAbove = "", string siblingBelow = "", string childField = "")
        {
            _selectedGOPublic = objectToInternalSelection;
            Transform parentTrnsfrmOfNewInternal = objectToInternalSelection.transform.parent;

            _selecIndex = objectToInternalSelection.transform.GetSiblingIndex();   //necessary??? idfk //nullref????????
            _displayedChildIndex = 0;
            _siblingsOfSelGO = objectToInternalSelection.GetAllSiblings();
            List<GameObject> internalChildList = _childGOList = objectToInternalSelection.ListChildrenOrNull();
            List<GameObject> currentSiblingsList = _siblingsOfSelGO;
            int selectionIndex = _selecIndex;

            //_selectedObject is a necessary fallback for when siblings haven't been loaded yet
            if (parentTrnsfrmOfNewInternal == null)        //removed ".gameObject" from this check so the code can actually check                             //This nullrefs when trying to select a parentless collider, in this case it was after having selected a different thing normally then selecting a ball
                parentField = "Fatherless";
            else
            {
                parentField = $"{parentTrnsfrmOfNewInternal.gameObject}";

                if (parentTrnsfrmOfNewInternal.childCount > 1)  //this nullrefs when selecting a parentless GO; why doesn't the nullcheck happen earlier tho
                    selectionIndex.StringAdjacentSiblingsElse(currentSiblingsList, out siblingAbove, out siblingBelow);
            }
            if (objectToInternalSelection.transform.childCount > 0)
                childField = $"{internalChildList[0]}";
            //nullref????
            _sgPropClass.RefreshScreen(parentField, siblingAbove, siblingBelow, childField);
        } //should probably set these differently depending



        public void ClearTerminal()
        {
            StopEditing();
            _selectedGOPublic = null;
            _childGOList = null;
            _siblingsOfSelGO = null;
            //_parentOfSelection = null;
            //_sgPropClass._sgpTxtGO_SibAboveOBSOLETE = null;  //how did this bastard nullref when it's literally not written wtf
            // _sgPropClass._sgpTxtGO_SibBelowOBSOLETE = null;
            _sgPropClass.RefreshScreen("choose...", "Select something!", "AwA", "Please select a collider", "Pick that one");
        }

        private void PlumbTheDepths()
        {
            bool otherSiblingsHaveChildren = _siblingsOfSelGO.Find(siblingsWithChildren => siblingsWithChildren.transform.childCount > 0);  // where did this even come from //i don't remember writing this line of code in Linq, but it had a typo where I was telling it to look in the Child list instead of in Siblings, no wonder it was screaming
            string warningToShriek = "Other siblings have children";
            if (!otherSiblingsHaveChildren)  //maybe subsequent ToChilds press on childless object should shrink _childGOList to only objects that have children?
                warningToShriek = "Cannot peer deeper";
            LogGoob.Scream(warningToShriek);
        }
        public void StopEditing()
        { }
        private void CopySiblingsToChildList()
        { _childGOList = _siblingsOfSelGO; }

    }

}
