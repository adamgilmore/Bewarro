namespace Bewarro.Netduino.LcdDisplay
{
    using System;
    using System.Collections;
    using System.IO.Ports;
    using System.Text;

    using Microsoft.SPOT;

    public class Page
    {
        private ArrayList _controls = new ArrayList();

        public ArrayList Controls
        {
            get { return _controls; }
        }
    }
}
