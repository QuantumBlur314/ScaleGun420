using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ScaleGun420
{
    public class GunInterfaces : ShipNotificationDisplay
    {


        protected List<String> _currentLayerSiblings;
        private bool _atBedrock;
        private bool _atSky;
        private bool _objAlreadySelected;
        private bool _targetHasSiblings;
        private string _selectedObject;
        private string _priorSelObject;



        public void Intake(string seenColliderToCpu)
        {
            if (seenColliderToCpu == _selectedObject)
            { return; }
            else if (_priorSelObject == null && _selectedObject != null)
            {
                _priorSelObject = _selectedObject;
                _selectedObject = seenColliderToCpu;
                    }
        }
        private void ScrollUp()
        {
        }
        private void ScrollDown()
        {
        }
        private void GetSiblings()
        {
        }
        private void ScrollSiblings()
        { }
        private bool CheckBedrock()
        {
            return false;
        }
        private void UpdatePropDisplay()
        { }
        private void TerminateSession()
        { }
    }
}
