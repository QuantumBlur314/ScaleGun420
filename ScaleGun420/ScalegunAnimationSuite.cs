using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;




///TO DO: 
/// A.)  use Campfire.CheckStickIntersection + RoastingStickController.Update as model for Staff Whacking ?
/// B.)  Create cursor placeholder with smoother animations using Mathf.SmoothStep(?) /less intensive/fewer beams? it gotta spin tho except when TheEditMode is rotating stuff, then for purely cosmetic reasons it should lock on
/// C.) Shake up which mode is which and possibly add another holster animation similar to how Solanum holds it for while you're _isDragging
/// D.)  Fix the fucked-up way you attach the tool to the cameraholster then duplicate the transform animations through both the Scalegun AND its holster object (it's weird and suboptimal and the sooner it's fixed the better)
namespace ScaleGun420
{
    public class ScalegunAnimationSuite : MonoBehaviour
    {
        protected ScalegunToolClass _toolIServe;
        public static Vector3 _toolBaseTransform;

        private void Awake()
        {
            LogGoob.WriteLine("ScalegunAnimationSuite is woke, grabbing ScalegunToolClass", OWML.Common.MessageType.Success);

            _toolIServe = Locator.GetPlayerBody().GetComponentInChildren<ScalegunToolClass>();
            _toolBaseTransform = _toolIServe.transform.localPosition;
        }

        public void ToolPositionalUpdate(ref bool isPuttingAwayField, ref bool isCenteredField, ref bool isEquippedField, float arrivalDegrees = 5f, DampedSpring3D moveSpringPositional = default)
        {
            float internalNum = (isPuttingAwayField ? Time.unscaledDeltaTime : Time.deltaTime);    //Wish there was a way to make this an external class, //040323_1733: There totally is, just make it an extension with inputtable ref/out parameters for the conditions you want.  cool resource, everyone will want to slob immediately from how hot and cool this is
            Vector3 internalVector3 = (isPuttingAwayField ? _toolIServe._stowTransform.localPosition : _toolIServe._holdTransform.localPosition);
            _toolIServe.transform.localPosition = moveSpringPositional.Update(_toolIServe.transform.localPosition, internalVector3, internalNum);
            float internalNum2 = Vector3.Angle(_toolIServe.transform.localPosition, internalVector3);

            if (isEquippedField && !isCenteredField && internalNum2 <= arrivalDegrees)  //if we're in edit mode, but not centered or at desired arrival degrees, then (p.s. base _arrivalDegrees is just a float of 5f, it's fine here) 
            { isCenteredField = true; }  //This might be causing trombles
            if (isPuttingAwayField && internalNum2 <= arrivalDegrees)
            {
                isEquippedField = false;
                isPuttingAwayField = false;
                //base.enabled = false;
                moveSpringPositional.ResetVelocity();
            }
        }
    }
}
