using OWML.Common;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace ScaleGun420
{

    /// <summary>
    /// MISSION UPDATE: Make _selectedObject ONLY update at the end of a navigation/coroutine; use in-method fields for everything else.  This cannot continue.
    /// </summary>
    public class SgComputer : MonoBehaviour
    {


        private ScalegunPropClass _sgPropClass;
        //private ScalegunToolClass _sgToolClass;

        private List<GameObject> _selGO_Siblings;
        private List<GameObject> _selGO_Children;
        ///public static ObservableCollection<GameObject> _observableCollectionTest;  //subscribe to the CollectionChanged event.  Event's arguments are NotifyCollectionChangedArgs.  Might be helpful idk

        private bool _babenCycleShouldRun = false;
        private Coroutine timerBabyCycle = null;

        public Coroutine timerLoadingChildren = null;
        public Coroutine siblingTimerCoroutine = null;

        private bool _cancelTimerChildren = false;
        private bool _cancelTimerSiblings = false;

        private float _timeLeftChildren;
        private float _timeLeftInSibTimerCorout;

        //public static GameObject _previousSelection;  //not used here, but ScalegunPropClass will use it to fill in adjacent UI fields without having to recalculate, //032323_1938: Actually this should probably be defined by the PropClass
        //private GameObject _parentOfSelection;
        public GameObject _selectedObject;  //no longer static, yay //040423_1905: This is now both "the selected object" AND a way of nullchecking "they DID selected object, tho"
        public GameObject _objectSelectedPublic;

        //private GameObject _previousSelection;  //_previousSELECTION IS DEAD YAYYYYYY

        public int _selObjIndex = 0;
        private int _arbitraryChildIndex = 0;




        private void Awake()
        {
            LogGoob.WriteLine("SgComputer is woke, grabbing ScalegunPropClass...", MessageType.Success);
            //_sgToolClass = Locator.GetPlayerBody().GetComponentInChildren<ScalegunToolClass>();
            _sgPropClass = Locator.GetPlayerBody().GetComponentInChildren<ScalegunPropClass>();
        }

        public enum IndexMarkerState
        {
            ToParentSecondScroll = -69,
        }
        private enum DelayLoadingOf
        {
            Siblings = 0,
            Children = 1,
        }

        public GameObject SelectedGOAtIndex()  //_selGO_Siblings starts null  
        {
            return GetGOAtIndex(_selObjIndex, _selGO_Siblings);
        }

        public GameObject GetGOAtIndex(int gOIndex, List<GameObject> inList)  //_selGO_Siblings starts null  
        {
            if (!DoesGOIndexExistInList(gOIndex, inList))
            { return null; }
            else                  //something is making empty _selGO_Siblings lists - like, not even containing _selObjIndex.  where is it coming from 
            {
                GameObject objectFromIndex = _selGO_Siblings[_selObjIndex];
                return objectFromIndex;
            }
        }

        private bool DoesGOIndexExistInList(int indexOfInterest, List<GameObject> listToCheck)
        {
            if (listToCheck == null || indexOfInterest < 0)
            {
                LogGoob.WriteLine($"GOIndex doesn't exist in list: indexOfInterest is {indexOfInterest}, listToCheck was {listToCheck} (if negative, check IndexMarkerStates)", MessageType.Warning);
                return false;
            }
            if (indexOfInterest > listToCheck.Count)
            {
                LogGoob.WriteLine($"INDEX BEYOND LIST RANGE: index of interest was {indexOfInterest}, but checked list only goes up to {listToCheck.Count}!", MessageType.Warning);
                return false;
            }
            if (listToCheck[indexOfInterest] != null)
                return true;
            else
                LogGoob.WriteLine("whar"); return false;
        }
        
        //Currently starts running on Up or DownSiblings if _timeLeftChildren isn't already running

        public void OnToParent()
        {
            if (_selObjIndex == (int)IndexMarkerState.ToParentSecondScroll || SelectedGOAtIndex().transform.parent.transform.parent == null)  //prevents it from scrolling to final parent layer, as there's no way to find siblings at the highest level
            {
                LogGoob.Scream("ERROR: CANNOT BREACH FIRMAMENT");
                return;
            }
            //THIS MUST UPDATE _selectedObect then work with it, there's NO OTHER OPTION.
            //FOR REFERENCE, _selGO_Siblings DOESN'T UPDATE UNTIL THE COROUTINE; _selGO_Siblings IS THE LIFELINE THAT _selObjIndex CLINGS TO HERE

            //The First Press
            else if (siblingTimerCoroutine == null)  //Doesn't require _selGO_Children to be defined actually, surprising
            {
                if (_selGO_Siblings == null)
                {
                    LogGoob.WriteLine("ToParent L369ish ERROR: _selGO_Siblings AND siblingTimerCoroutine are BOTH NULL; THE FOLLOWING GetCurrentSelection() WILL FLUB");
                    return;
                }
                //the below only happens if we have _selGO_Siblings, be not afraid.  GetCurrentSelection should work.  if it doesn't, bludgeon it with hammers
                GameObject gObjAtSelObjIndex = SelectedGOAtIndex(); //Updates _selectedObject for future use by the else{} statement below this "if"
                GameObject currentAfterFirstParentPress = gObjAtSelObjIndex.transform.parent.gameObject;

                //now begin the transition
                _arbitraryChildIndex = 0; //the current _arbitraryChildIndex is irrelevant since the original child list gets flushed regardless, because you're scrolling up the hierarchy 

                //________________HERE'S THE KICKER______________
                _selGO_Children = _selGO_Siblings; //The aforementioned flush lets us push the current siblings into the child field without issue,
                //________________HERE'S THE KICKER______________

                _arbitraryChildIndex = _selObjIndex; //And now we can tell it to select the one we were on, possibly needless but if Babens start Cycling somehow, it'll be happy
                _selectedObject = currentAfterFirstParentPress;  //we breakin free
                _selObjIndex = currentAfterFirstParentPress.transform.GetSiblingIndex();  //If no further scrolling happens, the coroutine will refresh _selGO_Siblings, and _selObjIndex will already know where it belongs in that list thanks to GetSiblingIndex()

                /// if (_selGO_Children != null) //<----- note: _selGO_Children WILL NEVER BE NULL ON THE FIRST ToParent PRESS REGARDLESS, UNLESS SOMETHING IS MAJORLY FUCKED

                _sgPropClass.UpdateScreenTextV2(
                    $"{currentAfterFirstParentPress.transform.parent}",
                    "SibTimer begin",
                    "SibTimer begin",
                    $"{gObjAtSelObjIndex}",
                    //current selection field override vvv
                    $"{currentAfterFirstParentPress} from currentAfterFirstParentPress"
                    );

                siblingTimerCoroutine = StartCoroutine(LoadSiblingsAfter(0.4f));  //handles the rest, retrieves fresh _selectedObject index if unavailable; also can probably use _selectObject unless u wanna fuckin... inject it as a parameter
            }

            //IF ADDITIONAL PRESSES HAPPEN BEFORE TIMER EXPIRES:
            else if (siblingTimerCoroutine != null)
            {
                _timeLeftInSibTimerCorout += 0.1f;

                bool scrollingSecondTime = (_selGO_Children == _selGO_Siblings);
                if (scrollingSecondTime)
                {
                    _selGO_Children = null;
                    _selObjIndex = (int)IndexMarkerState.ToParentSecondScroll;  //SET THIS NULL WHENEVER YOU NEED TO START FROM 0
                }
                ///  _arbitraryChildIndex = 0; //already handled by first press actually nvm

                var previousSelection = _selectedObject;
                _selectedObject = _selectedObject.transform.parent.gameObject;   //spamming 
                var nextParent = _selectedObject.transform.parent.gameObject;

                _sgPropClass.UpdateScreenTextV2(
                    $"{nextParent}",
                    "Timer Extended",
                    "Timer Extended",
                    $"{previousSelection} prevsel",
                    $"{_selectedObject}"
                    );
            }
        }

        //Does injecting a field into a parameter only set the parameter's initial value, or does it check the field every time the parameter's used in the method?  If _selectedObject changes between when this coroutine starts and when the timer runs out, will it use the CURRENT _selectedObject, or will it have the value _selectedObject had when the coroutine started?
        //040523_1749: Corby confirms it's just like setting a var - it's a one-time copying of the field's value at that moment, and doesn't update.
        private IEnumerator LoadSiblingsAfter(float time)  //if multiple things call this, ensure each variant waits for other variants to finish to avoid chaos
        {
            _timeLeftInSibTimerCorout = time;  //the start of a coroutine only happens once i think
            LogGoob.WriteLine("LoadSiblingsAfter ln184: this part of a coroutine only runs once, right?", MessageType.Success);
            while (_timeLeftInSibTimerCorout >= 0)
            {
                if (_cancelTimerSiblings)
                {
                    _cancelTimerSiblings = false;
                    break;
                }
                _timeLeftInSibTimerCorout -= Time.deltaTime;
                yield return null;
            }
            //ensures input gets eaten this frame
            yield return new WaitForEndOfFrame();

            //bool onlyScrolledUpOneLayer = (_selGO_Children == _selGO_Siblings);
            bool scrolledUpMultipleTimes = (_selObjIndex == (int)IndexMarkerState.ToParentSecondScroll);  //ACTUALLY CANNOT NECESSARILY BE USED TO GAUGE WHETHER HAS SCROLLED MULTIPLE TIMES
            string howToSetChildField = $"GetCurrentSelectionOugh_ax15";

            //the below happens once the timer is up:
            // if (onlyScrolledUpOneLayer) //This means the user only scrolled up one layer, thus the internal _selGO_Siblings  
            // { } //at this point, the following step is such a laughably small optimization i kinda want to cry
            if (scrolledUpMultipleTimes)
            {
                howToSetChildField = $"{_selectedObject}"; //probably maybe make this whole thingus its own method?  it'd have to pass out lots of fellas tho, it'd have to push lots of dudes through its 
                _selGO_Children = _selectedObject.GetAllChildren();
            }
            _selObjIndex = _selectedObject.transform.GetSiblingIndex();
            _selGO_Siblings = _selectedObject.GetAllSiblings();

            _sgPropClass.UpdateScreenTextV2(
                "SKIP",  //presumably already set by ToParent's base functions
                $"{_selObjIndex.AdjacentSiblingIn(_selGO_Siblings, 1)}",
                $"{_selObjIndex.AdjacentSiblingIn(_selGO_Siblings, -1)}",
                "SKIP", //also presumably already set by ToParent's base scrolling functions
                howToSetChildField
                );

            StopCoroutineStartBabies(ref siblingTimerCoroutine);
        }

        public void OnToChilds() //Add condition for scrolling to very bottom of the well
        {
            var currentSelection = SelectedGOAtIndex();
            if (currentSelection.transform.childCount == 0)  //MAYBE CHECK THE WaitBeforeLoadingChildren TIMER INSTEAD OF JUST WHETHER _selGO_Children==null
            {
                LogGoob.Scream("timerLoadingChildren isn't null!");
                return;
            }
            else
            {
                var childToSelect = _selGO_Children[_arbitraryChildIndex];
                string selectedChildName = childToSelect.ToString();
                var priorSelection = _selGO_Siblings[_selObjIndex];

                if (timerLoadingChildren == null)
                {
                    _selectedObject = SelectedGOAtIndex();  //vv  have different Coroutine fields for ToChild and ToSiblingInDirection?
                    timerLoadingChildren = StartCoroutine(LoadChildrenAfter(0.4f, _selectedObject));  //Coroutine saves the CURRENT _selectedObject to _oldSelObject; when _timerChildren runs out, will check whether _selectedObject is same as _old.  I think that means scrolling up and down with impunity wiil be fine, but again, sort the other garbage first
                }//Oh lord, ^^^^ Is starting two different coroutines under the same name gonna cause problems?  uh oh
                else
                { _timeLeftChildren += 0.1f; }  //this part makes little sense, i don't think it will ever be called 

                //you are laughably unequipped to visualize this the right way; why are you asking about grandchildren
                if (childToSelect.transform.childCount == 0)  //you should be able to dig down to the bottom.  WHAT DO YOU MEAN GetChild() BY INDEX ALREADY EXISTS
                {
                    var otherSiblingsHaveChildren = _selGO_Children.Find(siblingsWithChildren => siblingsWithChildren.transform.childCount > 0);
                    if (!otherSiblingsHaveChildren)  //maybe subsequent ToChilds press on childless object shrinks _selGO_Children to only objects that have children?
                    { LogGoob.Scream("Cannot peer deeper"); }
                    else { LogGoob.Scream("OnToChilds Ln251: Other siblings have children"); }
                }
                else if (childToSelect.transform.childCount > 0)
                {
                    _selGO_Siblings = _selGO_Children;
                    _selectedObject = _selGO_Siblings[_arbitraryChildIndex]; //Really wanna make _selectedObject local but The Brain Wall, The Bricks
                    _selObjIndex = _arbitraryChildIndex;
                    _arbitraryChildIndex = 0;
                    _selGO_Children = _selGO_Siblings[_selObjIndex].GetAllChildren();  //this might be unavoidable idfk
                    //_selGO_Children.Clear();
                    //_selGO_Children = null; //Try replacing this with the LoadChildrenAfter() Coroutine maybe?  fulfil your dreams
                }

                string siblingAbove = "";
                string siblingBelow = "";
                if (_selectedObject.transform.parent.transform.childCount > 1)
                {
                    siblingAbove = _selectedObject.AdjacentSiblingOfGOIn(_selGO_Siblings, 1).ToString();
                    siblingBelow = _selectedObject.AdjacentSiblingOfGOIn(_selGO_Siblings, -1).ToString();
                }

                _sgPropClass.UpdateScreenTextV2(   ///why must this bastard so insistently nullref
                    $"{priorSelection}, prevsel",
                    siblingAbove,
                    siblingBelow,
                    "pending update...",  // ____ .   .   .   Y   O   U   .   .   .____  ...are probably being overwritten by the stuff in LoadChildrenAfter
                    selectedChildName
                    );//CAN I GO TO A CHILD, THEN DELAY THE LOADING OF ITS CHILDREN UNTIL I SCROLL TO THE DESIRED SPOT?!  CAN I SUSPEND THE TIMER IN LIMBO LIKE THAT?!  //040223_2006: More trouble than it's worth for now, maybe later.  Just deal with that list being generated and pray it doesn't lag.
                      // _selGO_Children = null;  //DON'T DO THIS YET, WaitBeforeLoading CAN COMPARE _selGO_Children with _selGO_Siblings (a comparison probably more computationally intensive than it's worth tbh)
                      //if (timer == null)                      
                      //{ timer = StartCoroutine(WaitBeforeLoading(DelayLoadingOf.Children, 0.5f)); }
                      // else { LogGoob.WriteLine("ToolClass Ln236: ToChilds: Timer wasn't null; didn't start WaitBeforeLoading of Children, or do anything else"); }

            }
        }


        //for some reason, this never terminates because its time somehow stops above 0 and it stays in limbo
        private IEnumerator LoadChildrenAfter(float time, GameObject objectYouStartedAt = null)  //if multiple things call this, ensure each variant waits for other variants to finish to avoid chaos
        {
            _timeLeftChildren = time;   //WHY IN GOD'S NAME IS _timeLeftChildren STUCK ABOVE 0, THIS ISN'T JUST BECAUSE I DON'T _cancelTimerChildren ON UNEQUIP, IT'S HAPPENING WHILE EQUIPPED
            while (_timeLeftChildren >= 0)
            {
                if (_cancelTimerChildren)   //_cancelTimerChildren will stop the timer; remember to clear any values 
                {
                    _cancelTimerChildren = false;  //probably maybe cancel this with navigations that might conflict?  idk
                    break;
                }
                _timeLeftChildren -= Time.deltaTime;
                yield return null;
            }
            //ensures input gets eaten this frame
            yield return new WaitForEndOfFrame();

            //THROWN EXCEPTIONS CAUSE ANY METHOD, POSSIBLY INCLUDING COROUTINES, TO STOP IN THEIR TRACKS.  THAT MIGHT BE WHY THE COUNTER'S GETTING STUCK ABOVE ZERO

            GameObject currentObject = SelectedGOAtIndex();
            bool isBackAtSameObject = (objectYouStartedAt != null && currentObject == objectYouStartedAt);  //if objectYouStartedAt is null, isBackAtSameObject starts false, otherwise opposite
            int newChildIndex = 0;

            if (isBackAtSameObject)
            {
                LogGoob.WriteLine($"LoadChildrenAfter Ln309: setting newChildIndex to _arbitraryChildIndex {_arbitraryChildIndex}.  Current _selGO_Children count is {_selGO_Children.Count}, but MAY update within this frame", MessageType.Success);
                newChildIndex = _arbitraryChildIndex;  //wtf even IS _arbitraryChildIndex at this point?  should I just... idfk
            }
            else
            { _selGO_Children = currentObject.GetAllChildren(); } //Nullcheck this, dingus

            //ouhhh, if selObjIndex is ever set to -69 while this is also happening (something that would happen considering you wanted to use LoadChildrenAfter for ToChilds in addition to ToSiblingInDirection), this fails

            _arbitraryChildIndex = newChildIndex;  //newChildIndex exists specifically so _arbitraryChildIndex can be defined outside the brackets

            var firstChildAtNewChildIndex = _selGO_Children[newChildIndex]; //a nullref.  also this is all lagging to hell

            _sgPropClass.UpdateScreenTextV2("SKIP", "SKIP", "SKIP", $"{firstChildAtNewChildIndex}", "SKIP");  //040623_1222: _selGO_Children[newChildIndex] got an OutOfRangeException from something //Another nullref from scrolling up fast, seems to make subsequent vertical scrolls no longer update the child list
            //This has to run after either DelayLoadingOf.Children condition, so                          
            //If player loops back around and stops on same object, don't re-retrieve a new list of children

            StopCoroutineStartBabies(ref timerLoadingChildren);  //can confirm this corouttine wasn't stopping while the other one was, so i did this here too.
        }


        /// <summary>
        /// The below is NOT finishing its job, _selGO_Children isn't getting updated by scrolling up and down
        /// </summary>

        public void ToSiblingInDirection(int direction = 1)   //could probably microOptimize by splitting it up again and having different conditions using some weird hidden tags depending on whether a field was generated fresh or from prevSel, but no fuck you
        {
            GameObject newSelectionGO = new GameObject("SKIP");  //literally just do newSelectionGO.name you goofball

            string upperSibling = "SKIP";  //why am i doing it with strings here instead of objects, why am i like this
            string lowerSibling = "SKIP";

            if (_selGO_Siblings == null || _selGO_Siblings.Count <= 1)
            { upperSibling = "NO SIBLINGS"; lowerSibling = "NO SIBLINGS"; }

            else
            {
                ScrollThoseSiblings(direction, ref newSelectionGO, ref upperSibling, ref lowerSibling);
                if (timerLoadingChildren == null)
                {
                    //_selectedObject = GetSelGOFrom_selObjIndex();
                    var oldSelObject = SelectedGOAtIndex();  //caches current _selectedObject in _oldSelObject field in case _selectedObject ends up being same as it was (if you go UpSibling then DownSibling in rapid succession, this will probably break, don't test this until you sort other garbage first)
                    timerLoadingChildren = StartCoroutine(LoadChildrenAfter(0.4f, oldSelObject));  //Coroutine saves the CURRENT _selectedObject to _oldSelObject; when _timerChildren runs out, will check whether _selectedObject is same as _old.  I think that means scrolling up and down with impunity wiil be fine, but again, sort the other garbage first
                }
                else
                { _timeLeftChildren += 0.15f; }
            }
            _sgPropClass.UpdateScreenTextV2(
                "SKIP", upperSibling, lowerSibling, "SKIP", $"{newSelectionGO}");
        }


        private void ScrollThoseSiblings(int whichWay, ref GameObject brandSelectionGO, ref string topSibling, ref string bottomSibling)
        {  //LoadChildrenAfter doesn't use _selectedObject AT ALL; food for thought

            GameObject siblingFormerlyCurrent = _selGO_Siblings[_selObjIndex];  //Error, uh oh

            brandSelectionGO = _selObjIndex.AdjacentSiblingIn(_selGO_Siblings, whichWay);   //0323_1519: Idiot says this will always wrap around the list using "modulo" and Corby says to use .Count since .Count() will return Linq which is "stinky"

            int newSelectionIndex = brandSelectionGO.transform.GetSiblingIndex();

            _selObjIndex = newSelectionIndex;
            //_selectedObject = brandSelectionGO;  //disabled on a hunch aka mercy

            GameObject nextSibling = newSelectionIndex.AdjacentSiblingIn(_selGO_Siblings, whichWay);

            if (whichWay > 0)
            {
                topSibling = $"{nextSibling}";
                bottomSibling = $"{siblingFormerlyCurrent}, prevsel";
            }
            else if (whichWay < 0)
            {
                topSibling = $"{siblingFormerlyCurrent}, prevsel";
                bottomSibling = $"{nextSibling}";
            }
        }
        public void StopCoroutineStartBabies(ref Coroutine routine)  //this runs every frame in update, probably should move the bigger pulls to things that only happen sometimes
        {
            StopCoroutine(routine);
            routine = null;


            bool multipleBabiesInList = (_selGO_Children != null && _selGO_Children.Count > 1);
            if (!multipleBabiesInList)  //The below line is happening constantly because GetCurrentSelection is being called constantly for the check.  icky
            { return; }
            else if (multipleBabiesInList)
            {
                if (_babenCycleShouldRun == false)
                {
                    _babenCycleShouldRun = true;
                    if (timerBabyCycle == null)  //starts cycling through babies
                    { timerBabyCycle = StartCoroutine(CycleBabens(1)); }
                    else
                    { LogGoob.Scream("timingBabies are already born!"); }
                }
                return;
            }
            else if (siblingTimerCoroutine != null || timerLoadingChildren != null)
            {
                StopTheBabens();
            }
            return;

        }
        private void StartBabies()
        {
            bool multipleBabiesInList = (_selGO_Children != null && _selGO_Children.Count > 1);
            if (!multipleBabiesInList)  //The below line is happening constantly because GetCurrentSelection is being called constantly for the check.  icky
            { return; }
            else if (multipleBabiesInList)
            {
                if (_babenCycleShouldRun == false)
                {
                    _babenCycleShouldRun = true;
                    if (timerBabyCycle == null)  //starts cycling through babies
                    { timerBabyCycle = StartCoroutine(CycleBabens(1)); }
                    else
                    { LogGoob.Scream("timingBabies are already born!"); }
                }
                return;

            }
        }
        public void CleanDeadCoroutines()
        { }



        public IEnumerator CycleBabens(int upDown, float time = 1f)  //should probably make sure other children exist first
        {
            GameObject currentSelection = SelectedGOAtIndex();


            LogGoob.WriteLine("CycleBabens 444: which part of a coroutine loops, anyway?");
            if (_selGO_Children == null)
            {
                if (currentSelection.transform.childCount > 0)
                { LogGoob.WriteLine("CycleBabens 446: _selGO_Children is null, despite GOFromSelectionIndex having at least 1 child"); }
                else
                { LogGoob.WriteLine("CycleBabens 447: _selGOChildren is null, and GOFromSelectionIndex has no children"); }
                yield break;
            }
            else if (_selGO_Children.Count <= 1)
            {
                _sgPropClass.UpdateScreenTextV2("SKIP", "SKIP", "SKIP", $"{_selGO_Children[0]}");
                yield break;
            }
            else for (; ; )
                {
                    if (_selGO_Children == null)
                    { LogGoob.WriteLine("CycleBabens: coroutine was running while _selGO_Children was null.", MessageType.Warning); }
                    else
                    {
                        var newBaben = _arbitraryChildIndex.AdjacentSiblingIn(_selGO_Children, 1);  //maybe make lists a whole class component to attach to current index somehow?  idk if that's possible 
                        _arbitraryChildIndex = newBaben.transform.GetSiblingIndex();
                        _sgPropClass.UpdateScreenTextV2("SKIP", "SKIP", "SKIP", $"{newBaben}", "SKIP");
                    }
                    yield return new WaitForSeconds(time);
                }
        }
        public void StopTheBabens()
        {
            if (_babenCycleShouldRun == true || timerBabyCycle != null)
            {
                _babenCycleShouldRun = false;    //this will set _shouldBabens to false and deactivate the timingBabens coroutine when in edit mode
                StopCoroutine(timerBabyCycle);
                timerBabyCycle = null;
            }
            else
            {
                LogGoob.WriteLine("StopTheBabens L426: _babenCycleShouldRun was already false; StopoTheBabens but also wtf.");
            }
        }

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
                GameObject currentSelection = SelectedGOAtIndex(); //built-in nullcheck //never mind
                GameObject originalHitObject = hit.collider.gameObject;  //why do i have to nullcheck this too ffs //oh it's literally just when you aim at empty space ok
                GameObject newPickedObject = originalHitObject;



                if (originalHitObject.transform.parent.gameObject != null)
                { newPickedObject = originalHitObject.transform.parent.gameObject; }

                //vvv___vvv  IS _selectedObject NULL ALMOST EVER??????  vvv___vvv
                if (newPickedObject == currentSelection)  //probably make this internal, idk , trying to phase out _selectedObject in favor of index, but idk
                { return; }

                _arbitraryChildIndex = 0;
                _selGO_Children = newPickedObject.GetAllChildren();
                _selGO_Siblings = newPickedObject.GetAllSiblings();
                _selObjIndex = newPickedObject.transform.GetSiblingIndex();   //necessary??? idfk //nullref????????
                _selectedObject = newPickedObject;  //_selectedObject is a necessary fallback for when siblings haven't been loaded yet

                string siblingAbove = "";
                string siblingBelow = "";
                if (_selectedObject.transform.parent.transform.childCount > 1)
                {
                    siblingAbove = _selObjIndex.AdjacentSiblingIn(_selGO_Siblings, 1).ToString();
                    siblingBelow = _selObjIndex.AdjacentSiblingIn(_selGO_Siblings, -1).ToString();
                }
                _sgPropClass.UpdateScreenTextV2(       //nullref????
                    $"{newPickedObject.transform.parent}",
                    siblingAbove,  //should probably set these differently depending
                    siblingBelow,
                    $"{_selGO_Children[_arbitraryChildIndex]}"
                    );


            }
            //a new instance of this starts running fresh every time the staff fires; you were warned about this exact element of coroutines earlier yet here you are making the mistake again - surely by the second time you'd have adequate experience to know better!!!! smh fr, fr!!!!
        }

        public void ClearTerminal()
        {
            StopEditing();
            _selectedObject = null;
            _selGO_Children = null;
            _selGO_Siblings = null;
            //_parentOfSelection = null;
            //_sgPropClass._sgpTxtGO_SibAboveOBSOLETE = null;
           // _sgPropClass._sgpTxtGO_SibBelowOBSOLETE = null;
            _sgPropClass.UpdateScreenTextV2("choose...", "Select something!", "AwA", "Please select a collider", "Pick that one");
        }

        public void StopEditing()
        { }

    }

}
