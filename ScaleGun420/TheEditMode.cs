using System;
using System.Collections.Generic;
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

        private ScalegunPropClass _sEMProp;
        private SgComputer _sEMCpu;

        private void Awake()
        {
            _sEMProp = Locator.GetPlayerBody().GetComponentInChildren<ScalegunPropClass>();
            _sEMCpu = Locator.GetPlayerBody().GetComponentInChildren<SgComputer>();
        }
        private void GrabSelectedObject()
        {
            if (_sEMCpu.SelectedGOAtIndex() == null)
                return;
        }

    }

}
