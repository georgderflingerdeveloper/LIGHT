using System;
using Phidgets;
using Phidgets.Events;
using SystemServices;
using HomeControl.BASIC_COMPONENTS.Interfaces;

namespace BASIC_COMPONENTS
{
    public delegate void DigitalInputChanged( object sender, DigitalInputEventargs e );
    public delegate void DigitalOutputChanged( object sender, DigitalOutputEventargs e );

    public class DigitalInputEventargs : EventArgs
    {
        int _SerialNumber;
        int _index;
        bool _value;

        public int SerialNumber
        {
            get
            {
                return _SerialNumber;
            }

            set
            {
                _SerialNumber = value;
            }
        }

        public int Index
        {
            get
            {
                return _index;
            }

            set
            {
                _index = value;
            }
        }

        public bool Value
        {
            get
            {
                return _value;
            }

            set
            {
                _value = value;
            }
        }
    }

    public class DigitalOutputEventargs : EventArgs
    {
        int  _SerialNumber;
        int  _index;
        bool _value;

        public int SerialNumber
        {
            get
            {
                return _SerialNumber;
            }

            set
            {
                _SerialNumber = value;
            }
        }

        public int Index
        {
            get
            {
                return _index;
            }

            set
            {
                _index = value;
            }
        }

        public bool Value
        {
            get
            {
                return _value;
            }

            set
            {
                _value = value;
            }
        }
    }

    public enum HandlerMode
    {
        eHardware,
        eMocking
    }

    public class IOHandler : InterfaceKit, IIOHandler
    {
        #region DECLARATION
        const int WaitForAttachTime       = 2000;
        public event DigitalInputChanged  EDigitalInputChanged;
        public event DigitalOutputChanged EDigitalOutputChanged;
        DigitalInputEventargs             _DigitalInputEventargs   = new DigitalInputEventargs();
        DigitalOutputEventargs            _DigitalOutputEventargs  = new DigitalOutputEventargs();
        int  _inputIndex;
        bool _inputValue;
        int  _outputIndex;
        bool _outputValue;
        int _serialnumber;
        #endregion

        #region PROPERTIES
        public int InputIndex
        {
            get
            {
                return _inputIndex;
            }

            set
            {
                _inputIndex = value;
            }
        }

        public bool InputValue
        {
            get
            {
                return _inputValue;
            }

            set
            {
                _inputValue = value;
            }
        }

        public int OutputIndex
        {
            get
            {
                return _outputIndex;
            }

            set
            {
                _outputIndex = value;
            }
        }

        public bool OutputValue
        {
            get
            {
                return _outputValue;
            }

            set
            {
                _outputValue = value;
            }
        }
        #endregion

        #region PRIVATEMETHODS
        void Constructor( HandlerMode mode, int serialnumber )
        {
            _serialnumber = serialnumber;
            switch( mode )
            {
               case HandlerMode.eMocking:
                    return;

               case HandlerMode.eHardware:
                    try
                    {
                        if( serialnumber == 0 )
                        {
                            base.open( );
                        }
                        else if( serialnumber > 0)
                        {
                            base.open( serialnumber );
                        }
                        base.waitForAttachment( WaitForAttachTime );
                        base.InputChange  += IOHandler_InputChange;
                        base.OutputChange += IOHandler_OutputChange;
                    }
                    catch( Exception ex )
                    {
                        Services.TraceMessage_( ex.Message );
                    }
                    break;

            }
            if( base.Attached )
            {
                IOHardwareInformation( );
            }
        }

        void IOHardwareInformation()
        {
            Console.WriteLine( TimeUtil.GetTimestamp( ) + " IO card Type:  "          + base.Type.ToString() );
            Console.WriteLine( TimeUtil.GetTimestamp( ) + " IO card serial number:  " + base.SerialNumber.ToString() );
        }

        #endregion

        #region CONSTRUCTOR
        public IOHandler( HandlerMode mode ) : base( )
        {
            Constructor( mode, 0 );
        }

        public IOHandler( HandlerMode mode, int serialnumber ) : base( )
        {
            Constructor( mode, serialnumber );
        }

        #endregion

        #region PUBLICMETHODS       
        public void UpdateDigitalOutputs( int index, bool value)
        {
            if( base.Attached )
            {
                if( index >= 0 && index < outputs.Count )
                {
                    base.outputs[index] = value;
                    _outputIndex = index;
                    _outputValue = value;
                }
            }
        }

        public void SetAllOutputs( bool value )
        {
            if( base.Attached )
            {
                for( int i = 0; i < base.outputs.Count; i++ )
                {
                    base.outputs[i] = value;
                }
            }
        }

        public int GetNumberOfAvailableInputs()
        {
            int count = 0;
            if( base.outputs != null )
            {
                count =  base.outputs.Count;
            }
            return ( count );
        }
        #endregion

        #region EVENTHANDLERS
        private void IOHandler_InputChange( object sender, InputChangeEventArgs e )
        {
            if( base.Attached )
            {
                _DigitalInputEventargs.Index        = e.Index;
                _DigitalInputEventargs.Value        = e.Value;
                _DigitalInputEventargs.SerialNumber = _serialnumber;
                _inputIndex                         = e.Index;
                _inputValue                         = e.Value;
            }
            else // for mocking usage
            {
                _DigitalInputEventargs.Index = _inputIndex;
                _DigitalInputEventargs.Value = _inputValue;
            }
            EDigitalInputChanged?.Invoke( this, _DigitalInputEventargs );
        }

        private void IOHandler_OutputChange( object sender, OutputChangeEventArgs e )
        {
            if( base.Attached )
            {
                _DigitalOutputEventargs.Index = e.Index;
                _DigitalOutputEventargs.Value = e.Value;
                _DigitalOutputEventargs.SerialNumber = _serialnumber;
            }
            else  // for mocking usage
            {
                _DigitalOutputEventargs.Index = _outputIndex;
                _DigitalOutputEventargs.Value = _outputValue;
            }
            EDigitalOutputChanged?.Invoke( this, _DigitalOutputEventargs );
        }
        #endregion
    }
}
