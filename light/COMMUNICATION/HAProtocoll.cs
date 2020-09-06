using System;
using System.Collections.Generic;
using System.Linq;
using HomeAutomation.HardConfig_Collected;
using SystemServices;
using HomeAutomationProtocoll;

namespace Communication
{
    namespace HAProtocoll
    {
        #region IO_CONTROL

        public class IOSelection
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "dig")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
            public delegate void digInput( int index, bool value );
            public event digInput EDigitalInput;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "dig")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
            public delegate void digOutput( int index, bool value );
            public event digOutput EDigitalOutput;

            // true -got IO message
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
            public void Is_IOMessage( bool info)
            {
                IsIOMessage_ = info;
            }
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
            public void DigitalOutput_Index( int ind ) 
            {
                DigitalOutputIndex_ = ind;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
            public void DigitalOutput_Value( bool value)
            {
                DigitalOutputValue_ = value;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
            public void DigitalOutput_IndexWithValue( int ind, bool val )
            {
                DigitalOutputIndex_ = ind;
                DigitalOutputValue_ = val;

                EDigitalOutput?.Invoke( ind, val );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
            public void DigitalInput_IndexWithValue( int ind, bool val )
            {
                DigitalInputIndex_ = ind;
                DigitalInputValue_ = val;

                if( EDigitalInput != null )
                {
                    EDigitalInput( ind, val );
                }
            }

            bool  IsIOMessage_;    
            int   DigitalOutputIndex_; 
            bool  DigitalOutputValue_;
            int   DigitalInputIndex_;
            bool  DigitalInputValue_;

            public bool IsIOMessage        { get { return IsIOMessage_;        } } // true -got IO message
            public int  DigitalOutputIndex { get { return DigitalOutputIndex_; } }
            public bool DigitalOutputValue { get { return DigitalOutputValue_; } }
            public int  DigitalInputIndex  { get { return DigitalInputIndex_;  } }
            public bool DigitalInputValue  { get { return DigitalInputValue_;  } }
        }

        public class IOState
        {
            public bool IsIOSingle        { get; set; }
            public bool IsInput           { get; set; }
            public bool IsOutput          { get; set; }
            public bool IsIOState         { get; set; }
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
            public bool[] DigitalInputs   { get; set; }
            public int  SingleInputIndex  { get; set; }
            public bool SingleInputValue  { get; set; }
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
            public bool[] DigitalOutputs  { get; set; }
            public int SingleOutputIndex  { get; set; }
            public bool SingleOutputValue { get; set; } 
        }

        #endregion

        #region DEFINITIONES
        static class RemoteStationDefinition
        {
            public const string SERVER                                              = "SERVER";
            public const string CLIENT                                              = "CLIENT";
        }
        static class LinkingWords
        {
            public const string TO                                                  =  "TO";
            public const string FROM                                                =  "FROM";
        }
        static class MessageTyp
        {
            // f.e send one single coamndo to a client
            public const string IO_COMAND                                           = "IOSINGLECOMMAND";
            // ask client about the current IO state
            public const string IO_WHATSUP                                          = "IOWHATSUP";
            // quick status for on 16/16 IO Primer
            public const string IO_QUICKSTATE_PRIMER                                = "IOQUICKSTATEPRIMER";
            // status input
            public const string IO_SINGLE_INPUT_STATUS                              = "IOSINGLEINPUTSTATUS";
            // status output
            public const string IO_SINGLE_OUTPUT_STATUS                             = "IOSINGLEOUTPUTSTATUS";
            // scheduler
            public const string SCHEDULER_JOB                                       = "SCHEDULERJOB";

            public const string ASK_SCHEDULER_JOB_STATUS                            = "ASKFORSCHEDULERSTATUSJOB";

            public const string HACOMAND                                            = "HACOMMAND";

            public const string ASK_FORVERSION                                      = "ASKFORVERSION";
        }

        public static class HomeAutomationCommandos
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "ALL")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "LIGHTS")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "OFF")]
            public const string ALL_LIGHTS_OFF                               = "ALLIGHTSOFF";
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "ALL")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "LIGHTS")]
            public const string ALL_LIGHTS_ON                                = "ALLIGHTSON";
        }

        public static class HomeAutomationAnswers
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "ANSWER")]
            public const string ANSWER = "ANSWEROF";
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "ANSWER")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SCHEDULER")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "STATUS")]
            static public readonly string  ANSWER_SCHEDULER_STATUS = ANSWER + "_" + MessageTyp.ASK_SCHEDULER_JOB_STATUS;
        }


        public static class Section
        {
            public const string  GalleryFloor                                       = "GALLERYFLOOR";
            public const string  GalleryCeiling                                     = "GALLERYCEILING";
            public const string  RoofRoomFloor                                      = "ROOFROOMFLOOR";
        }
        #endregion

        #region MESSAGE_HANDLING
        public class MessageAnalyzer
        {
            #region DECLARATION

            decimal previousTransactionNumber = 0;
            decimal TransactionNumber         = 0;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
            public delegate void ProcessHACommando( object sender, string section, string commando );
            public event ProcessHACommando EProcessHACommando;

            #endregion

            public MessageAnalyzer( )
            {
            }

            #region MESSAGE_INDEX
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "0#")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Argumente von öffentlichen Methoden validieren", MessageId = "0")]
            public static int GetDeviceIndex( ref Dictionary<string,int> dic, string key )
            {
                // Try to get the result in the static Dictionary
                int result;
                if( dic.TryGetValue( key, out result ) )
                {
                    return result;
                }
                else
                {
                    return GeneralConstants.DeviceNotFound;
                }
            }
            // index 0  ... message length
            // index 1  ... expecting timestamp
            // index 2  ... expecting transaction number
            // index 3  ... expecting message typ
            // index 4  ... expecting server or client
            // index 5  ... expecting TO
            // index 6  ... expecting device
            // index 7  ... expecting value
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
            public static class ExpectedComandMessageIndex
            {
                static int                 index                = 0;
                public static readonly int IndMessageLength     = index++;
                public static readonly int IndTimestamp         = index++;
                public static readonly int IndTransactionNumber = index++;
                public static readonly int IndMessageTyp        = index++;
                public static readonly int IndServerClient      = index++;
                public static readonly int IndTO                = index++;
                public static readonly int IndDevice            = index++;
                public static readonly int IndValue             = index++;
            }

            // index 0      ... expecting message length
            // index 1      ... expecting transaction number
            // index 2      ... expecting timestamp 1
            // index 3      ... expecting timestamp 2
            // index 3      ... client name
            // index 3      ... "says"
            // index 3      ... expecting room
            // index 4      ... expecting message typ
            // index 5      ... expecting INPUT
            // index 6..22  ... expecting status inpts
            // index 23     ... expecting OUTPUT
            // index 24..40 ... expecting status outputs
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
            public static class ExpectedQuickAnswerMessageIndex
            {
                static int index                                = 0;
                public static readonly int IndMessageLength     = index++;   //0
                public static readonly int IndTransactionNumber = index++;   //1
                public static readonly int IndTimestamp1        = index++;   //2
                public static readonly int IndTimestamp2        = index++;   //3
                public static readonly int CLientName           = index++;   //4
                public static readonly int Says                 = index++;   //5
                public static readonly int StrEmpty1            = index++;   //6
                public static readonly int IndMessageTyp        = index++;   //7
                public static readonly int Room                 = index++;   //8
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "INPUT")]
                public static readonly int IndINPUT             = index++;   //9
                public static readonly int StrEmpty2            = index++;   //10
                public static readonly int IndInputs            = index;
                public static readonly int StrEmpty3            = index +  GeneralConstants.NumberOfInputsIOCard;
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "OUTPUT")]
                public static readonly int IndOUTPUT            = index += GeneralConstants.NumberOfInputsIOCard + 1;
                public static readonly int StrEmpty4            = index +  GeneralConstants.NumberOfOutputsIOCard;
                public static readonly int IndOutputs           = index + 2;
                public static readonly int StrEmpty5            = IndOutputs +  GeneralConstants.NumberOfOutputsIOCard - 1;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
            public static class ExpectedSingleIOAnswerMessageIndex
            {
                static int index                                = 0;
                public static readonly int IndMessageLength     = index++;  //0
                public static readonly int IndTransactionNumber = index++;  //1
                public static readonly int IndTimestamp1        = index++;  //2
                public static readonly int IndTimestamp2        = index++;  //3
                public static readonly int IndCLientName        = index++;  //4
                public static readonly int IndSays              = index++;  //5
                public static readonly int IndStrEmpty1         = index++;  //6
                public static readonly int IndMessageTyp        = index++;  //
                public static readonly int IndRoom              = index++;  //
                public static readonly int IndTo                = index++;
                public static readonly int IndServer            = index++;
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Io")]
                public static readonly int IndIoTyp             = index++;
                public static readonly int IndStrEmpty2         = index++;
                public static readonly int IndIndex             = index++;
                public static readonly int IndValue             = index++;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
            public static class ExpectedMessageIndex
            {
                static int                 index                = 0;          // 0
                public static readonly int IndMessageLength     = index++;    // 0
                public static readonly int IndTimestamp         = index++;    // 1
                public static readonly int IndTransactionNumber = index++;    // 2
                public static readonly int IndMessageTyp        = index++;    // 3
                public static readonly int IndServerClient      = index++;    // 4
                public static readonly int IndTO                = index++;    // 5
                public static readonly int IndDevice            = index++;    // 6
                public static readonly int IndNext              = index;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
            public static class ExpectedMessageIndexShort
            {
                static int                 index                = 0;          // 0
                public static readonly int IndMessageLength     = index++;    // 0
                public static readonly int IndTimestampDate     = index++;    // 1
                public static readonly int IndTimestampTime     = index++;    // 2
                public static readonly int IndTransactionNumber = index++;    // 3
                public static readonly int IndMessageTyp        = index++;    // 4
                public static readonly int IndNext              = index;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
            public static class ExpectedMessageIndexHACommandos
            {
                static int                 index                = ExpectedMessageIndexShort.IndNext;
                public static readonly int IndSection           = index++; // 5
                public static readonly int IndComando           = index++; // 6
            }

            // EXAMPLE:
            // 0108 27042015:16h02m27s694ms 000000057_SCHEDULERJOB_SERVER_TO_Boiler_1_Start_01:01:01_02:02:02_FromNow
            //   1              2               3           4         5   6   7     8  9      10     11         12
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
            public static class ExpectedSchedulerCommandIndex
            {
                static int                 index                = ExpectedMessageIndex.IndNext;
                public static readonly int IndJobId             = index++; // 7
                public static readonly int IndComando           = index++; // 8
                public static readonly int IndStartTime         = index++; // 9
                public static readonly int IndStopTime          = index++; // 10
                public static readonly int IndDays              = index++; // 11
            }

            //0076 21052015:15h47m20s906ms 000000000_ASKFORSCHEDULERSTATUSJOB_Boiler_1
            // 1           2                  3           4                     5    6
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
            public static class ExpectedSchedulerInquiryIndex
            {
                static int                 index                = 0;
                public static readonly int IndMessageLength     = index++;
                public static readonly int IndTimestamp         = index++;
                public static readonly int IndTransactionNumber = index++;
                public static readonly int IndMessageTyp        = index++;
                public static readonly int IndJob               = index++;
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "ID")]
                public static readonly int IndJobID             = index++;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
            public static class ExpectedSchedulerAnswerIndex
            {
                //static int                 index                = 0;
            }
            #endregion

            #region ANALYZER_METHODS

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Convert.ToBoolean(System.String)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Argumente von öffentlichen Methoden validieren", MessageId = "0")]
            public IOSelection AnalyzeAnyIOComando( string message )
            {
                IOSelection IOSelection_ = new IOSelection( );
                string[] message_ = message.Split(  ComandoString.Telegram.Seperator,' ' );
                if( decimal.TryParse( message_[ExpectedComandMessageIndex.IndTransactionNumber], out TransactionNumber ) )
                {
                }
                else
                {
                    return IOSelection_;
                }

                // TODO
                if( previousTransactionNumber != TransactionNumber )
                {
                    previousTransactionNumber = TransactionNumber;
                }

                if( message_[ExpectedComandMessageIndex.IndMessageTyp] == MessageTyp.IO_COMAND )
                {
                    IOSelection_.Is_IOMessage( true );
                }
                else
                {
                    IOSelection_.Is_IOMessage( false );
                    return IOSelection_;
                }

                IOSelection_.DigitalOutput_Index( GetDeviceIndex( ref HADictionaries.DeviceDictionaryCenterdigitalOut, message_[ExpectedComandMessageIndex.IndDevice] ) );
                IOSelection_.DigitalOutput_Value( Convert.ToBoolean( message_[ExpectedComandMessageIndex.IndValue] ));

                return IOSelection_;
            }

            // server asks client - "whatsup with your IO´s"
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Argumente von öffentlichen Methoden validieren", MessageId = "0")]
            public bool AnalyzeIOStateInquiry( string message )
            {
                bool result = false;

                if( message.Contains( MessageTyp.IO_WHATSUP ) )
                {
                    return ( result = true );
                }
                return ( result );
            }

            // client answers with a quick status information
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Argumente von öffentlichen Methoden validieren", MessageId = "0")]
            static public bool AnalyzeIOStateInquiryAnswer( string message )
            {
                bool result = false;

                if( message.Contains( MessageTyp.IO_QUICKSTATE_PRIMER ) )
                {
                    return ( result = true );
                }
                return ( result );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Argumente von öffentlichen Methoden validieren", MessageId = "0")]
            static public bool AnalyseSingleIOEvent( string message )
            {
                bool result = false;

                if( message.Contains( MessageTyp.IO_SINGLE_INPUT_STATUS ) || message.Contains( MessageTyp.IO_SINGLE_OUTPUT_STATUS ) )
                {
                    return ( result = true );
                }
                return ( result );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Argumente von öffentlichen Methoden validieren", MessageId = "0")]
            static public bool AnalyseSchedulerComands( string message )
            {
                bool result = false;

                if( message.Contains( MessageTyp.SCHEDULER_JOB ) )
                {
                    return ( result = true );
                }
                return ( result );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Convert.ToInt16(System.String)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Convert.ToBoolean(System.String)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
            static public IOState GetIOStateInquiryAnswer( string message )
            {
                IOState IO_State        = new IOState( );
                IO_State.DigitalInputs  = new bool[GeneralConstants.NumberOfInputsIOCard];
                IO_State.DigitalOutputs = new bool[GeneralConstants.NumberOfOutputsIOCard];

                if( AnalyzeIOStateInquiryAnswer( message ) )
                {
                    IO_State.IsIOState = true;
                    string[] messageparts = message.Split( Seperators.delimiterChars );
                    int j = 0;
                    for( int i =  ExpectedQuickAnswerMessageIndex.IndInputs; i <  ExpectedQuickAnswerMessageIndex.StrEmpty3; i++  )
                    {  
                        IO_State.DigitalInputs[j] =  ( messageparts[i] == "1" ) ? true : false;

                        if( messageparts[i] != "1" && messageparts[i] != "0" )
                        {
                            IO_State.IsIOState = false;
                            throw new Exception( "Home automation application protocoll IO answer does not contain 1 or 0" );
                        }
                        j++;
                    }
                    j = 0;
                    for( int i =  ExpectedQuickAnswerMessageIndex.IndOutputs; i <  ExpectedQuickAnswerMessageIndex.StrEmpty5; i++ )
                    {
                        IO_State.DigitalOutputs[j] =  ( messageparts[i] == "1" ) ? true : false;

                        if( messageparts[i] != "1" && messageparts[i] != "0" )
                        {
                            IO_State.IsIOState = false;
                            throw new Exception( "Home automation application protocoll IO answer does not contain 1 or 0" );
                        }
                        j++;
                  }
                }
                else
                {
                    IO_State.IsIOState = false;
                }

                if( AnalyseSingleIOEvent( message ) )
                {
                    IO_State.IsIOSingle = true;
                    IO_State.IsInput    = false;
                    IO_State.IsOutput   = false;
                    string[] messageparts = message.Split( Seperators.delimiterChars );
                    switch( messageparts[ExpectedSingleIOAnswerMessageIndex.IndIoTyp] )
                    {
                        case GeneralConstants.DigitalInput_:
                             IO_State.IsInput    = true;
                             IO_State.SingleInputIndex = Convert.ToInt16( messageparts[ExpectedSingleIOAnswerMessageIndex.IndIndex] );
                             IO_State.SingleInputValue = Convert.ToBoolean( messageparts[ExpectedSingleIOAnswerMessageIndex.IndValue] );
                             break;
                        case GeneralConstants.DigitalOutput_:
                             IO_State.IsOutput    = true;
                             IO_State.SingleOutputIndex = Convert.ToInt16( messageparts[ExpectedSingleIOAnswerMessageIndex.IndIndex] );
                             IO_State.SingleOutputValue = Convert.ToBoolean( messageparts[ExpectedSingleIOAnswerMessageIndex.IndValue] );
                             break;
                    }
                }
                else
                {
                    IO_State.IsIOSingle = false;
                }
                return ( IO_State );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Argumente von öffentlichen Methoden validieren", MessageId = "0")]
            public void AnalyzeHACommando( string message )
            {
                if( message.Contains( MessageTyp.HACOMAND ) )
                {
                    string[] messageparts = message.Split( Seperators.delimiterCharsExtended );
                    if( EProcessHACommando != null )
                    {
                        EProcessHACommando( this, messageparts[ExpectedMessageIndexHACommandos.IndSection], 
                                                  messageparts[ExpectedMessageIndexHACommandos.IndComando] );
                    }
                }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Argumente von öffentlichen Methoden validieren", MessageId = "0")]
            public bool AnalyzeVersionInquiry( string message )
            {
                bool result = false;

                if( message.Contains( MessageTyp.ASK_FORVERSION ) )
                {
                    return ( result = true );
                }
                return ( result );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Argumente von öffentlichen Methoden validieren", MessageId = "0")]
            static public bool AnalyzeSchedulerStatusInquiry( string message )
            {
                bool result = false;

                if( message.Contains( MessageTyp.ASK_SCHEDULER_JOB_STATUS ) )
                {
                    return ( result = true );
                }
                return ( result );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Argumente von öffentlichen Methoden validieren", MessageId = "1")]
            static public bool AnalyzeCommonCommando( string commando, string message )
            {
                bool result = false;

                if( message.Contains( commando ) )
                {
                    return ( result = true );
                }
                return ( result );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "part")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Argumente von öffentlichen Methoden validieren", MessageId = "0")]
            public static string GetMessagePartAfterKeyWord( string message, string keyword )
            {
                int index = 0;
                string result = "";
                string[] parts = message.Split( Seperators.delimiterCharsExtended );

                foreach( string part in parts )
                {
                    if( parts[index++] == keyword )
                    {
                        break;
                    }
                }

                if( index < parts.Count() )
                {
                    if( !String.IsNullOrEmpty( parts[index] ) )
                    {
                        result = parts[index];
                    }
                }

                return ( result );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "part")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Argumente von öffentlichen Methoden validieren", MessageId = "0")]
            public static string GetMessagePartAfterKeyWord( string message, string keyword, int indexDesiredPart )
            {
                int index = 0;
                string result = "";
                string[] parts = message.Split( Seperators.delimiterCharsExtended );

                foreach( string part in parts )
                {
                    if( parts[index++] == keyword )
                    {
                        break;
                    }
                }

                if( index + indexDesiredPart < parts.Count() )
                {
                    if( !String.IsNullOrEmpty( parts[index + indexDesiredPart] ) )
                    {
                        result = parts[index + indexDesiredPart];
                    }
                }

                return ( result );
            }


            #endregion
        }

        static class MessageBuilder
        {
            public static char Seperator = '_';

            static class IOChannel
            {
                public const string OUT                                                = "OUTPUTS:";
                public const string IN                                                 = "INPUTS:";
            }

            #region MESSAGE_BUILDER_METHODS
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "SystemServices.Services.TraceMessage(System.String,System.String,System.String,System.Int32)")]
            public static string BuildIOStateDebugMessage( string device, bool[] input, bool[] output )
            {
                if( input.Length  > GeneralConstants.NumberOfInputsIOCard ||
                    output.Length > GeneralConstants.NumberOfOutputsIOCard )
                {
                    SystemServices.Services.TraceMessage( "Array parameter sizes don´t match with definition" );
                }

                string sinput="";
                for( int i=0; i<input.Length; i++ )
                {
                    sinput += i.ToString( );
                    sinput += Seperator;
                    sinput += input[i].ToString( );
                    sinput += EscapeSequences.CRLF;
                }

                string soutput="";
                for( int i=0; i<output.Length; i++ )
                {
                    soutput += i.ToString( );
                    soutput += Seperator;
                    soutput += output[i].ToString( );
                    soutput += EscapeSequences.CRLF;
                }

                return ( device                                  + 
                         Seperator                               +
                         LinkingWords.TO                         +
                         Seperator                               +
                         RemoteStationDefinition.SERVER          +
                         EscapeSequences.CRLF                    +
                         IOChannel.IN                            +
                         EscapeSequences.CRLF                    +
                         sinput                                  +
                         IOChannel.OUT                           +
                         EscapeSequences.CRLF                    +
                         soutput
                       );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "SystemServices.Services.TraceMessage(System.String,System.String,System.String,System.Int32)")]
            public static string BuildIOStateMessageInFullText( string room, bool[] input, bool[] output, ref Dictionary<string, int> deviceOut, ref Dictionary<string, int> deviceIn)
            {
                if( input.Length  > GeneralConstants.NumberOfInputsIOCard ||
                    output.Length > GeneralConstants.NumberOfOutputsIOCard )
                {
                    SystemServices.Services.TraceMessage( "Array parameter sizes don´t match with definition" );
                    return ( "" );
                }
                string sinput="";
                for( int i=0; i<input.Length; i++ )
                {
                    sinput += i.ToString( );
                    sinput += FormatString.SpaceHolder1;
                    sinput += HADictionaries.GetKeyByValue( ref deviceIn, i );
                    sinput += FormatString.SpaceHolder1;
                    sinput += input[i].ToString( );
                    sinput += FormatString.SpaceHolder1;
                    sinput += EscapeSequences.CRLF;
                    sinput += FormatString.SpaceHolder1;
                }

                string soutput="";
                for( int i=0; i<output.Length; i++ )
                {
                    soutput += i.ToString( );
                    soutput += FormatString.SpaceHolder1;
                    soutput += HADictionaries.GetKeyByValue( ref deviceOut, i ); // gets devicename
                    soutput += FormatString.SpaceHolder1;
                    soutput += output[i].ToString( );
                    soutput += FormatString.SpaceHolder1;
                    soutput += EscapeSequences.CRLF;
                    soutput += FormatString.SpaceHolder1;
                }

                return ( MessageTyp.IO_WHATSUP                   +
                         FormatString.SpaceHolder1               +
                         room                                    + 
                         FormatString.SpaceHolder1               +
                         LinkingWords.TO                         +
                         FormatString.SpaceHolder1               +
                         RemoteStationDefinition.SERVER          +
                         EscapeSequences.CRLF                    +
                         IOChannel.IN                            +
                         EscapeSequences.CRLF                    +
                         sinput                                  +
                         IOChannel.OUT                           +
                         EscapeSequences.CRLF                    +
                         soutput
                       );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", MessageId = "SystemServices.Services.TraceMessage(System.String,System.String,System.String,System.Int32)")]
            public static string BuildIOStateMessageSimple( string room, bool[] input, bool[] output )
            {
                if( (input == null) || (output == null) )
                {
                    return "";
                }
                if( input.Length  > GeneralConstants.NumberOfInputsIOCard ||
                    output.Length > GeneralConstants.NumberOfOutputsIOCard )
                {
                    SystemServices.Services.TraceMessage( "Array parameter sizes don´t match with definition" );
                    return ( "" );
                }

                string InputStatus = "";

                for( int i=0; i < input.Length; i++ )
                {
                    InputStatus += input[i] ? "1" : "0"; 
                    InputStatus += FormatString.SpaceHolder1;
                }

                string OutputStatus = "";

                for( int i=0; i < output.Length; i++ )
                {
                    OutputStatus += output[i] ? "1" : "0";
                    OutputStatus += FormatString.SpaceHolder1;
                }

                return (  
                          MessageTyp.IO_QUICKSTATE_PRIMER +
                          FormatString.SpaceHolder1       +
                          room                            +
                          FormatString.SpaceHolder1       +
                          IOChannel.IN                    +
                          FormatString.SpaceHolder1       +
                          InputStatus                     +
                          FormatString.SpaceHolder1       +
                          IOChannel.OUT                   + 
                          FormatString.SpaceHolder1       +
                          OutputStatus                          
                       );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public static string BuildIOSingleComandoMessage( string device, bool Value )
            {
                return ( MessageTyp.IO_COMAND                    +
                         Seperator                               +
                         RemoteStationDefinition.SERVER          + 
                         Seperator                               +
                         LinkingWords.TO                         +
                         Seperator                               +
                         device                                  +   // device f.e. boiler, heater, light1, ....
                         Seperator                               +
                         Value.ToString( )
                       );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public static string BuildSchedulerJobMessage( string device, string Value )
            {
                return ( MessageTyp.SCHEDULER_JOB            +
                         Seperator                           +
                         RemoteStationDefinition.SERVER      +
                         Seperator                           +
                         LinkingWords.TO                     +
                         Seperator                           +
                         device                              +
                         Seperator                           +
                         Value
                       );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public static string BuildIOSingleEventMessage( string device, int index, bool Value )
            {
                return ( MessageTyp.IO_COMAND                    +
                         FormatString.SpaceHolder1               + 
                         device                                  +
                         FormatString.SpaceHolder1               +
                         LinkingWords.TO                         +
                         FormatString.SpaceHolder1               +
                         RemoteStationDefinition.SERVER          +
                         FormatString.SpaceHolder1               +
                         index.ToString()                        +
                         FormatString.SpaceHolder1               +
                         Value.ToString( )
                       );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
            public static string BuildIOSingleEventMessage( string msgtyp, string room, int index, bool Value, string iotyp )
            {
                return ( msgtyp                                  +
                         FormatString.SpaceHolder1               +
                         room                                    + 
                         FormatString.SpaceHolder1               +
                         LinkingWords.TO                         +
                         FormatString.SpaceHolder1               +
                         RemoteStationDefinition.SERVER          +
                         FormatString.SpaceHolder1               +
                         iotyp                                   +
                         FormatString.SpaceHolder1               +
                         index.ToString()                        +
                         FormatString.SpaceHolder1               +
                         Value.ToString( )
                       );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public static string BuildIOSingleComandoMessage( ref decimal transactionnumber, string device, bool Value )
            {
                string transaction = String.Format( FormatConstants.TransactionNumberFormat, transactionnumber );
                return ( transaction                             +
                         Seperator                               +
                         BuildIOSingleComandoMessage( device,  Value )
                       );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public static string BuildIOSingleComandoMessageWithTimeStamp( ref decimal transactionnumber, string device, bool Value )
            {
                string transaction = String.Format( FormatConstants.TransactionNumberFormat, transactionnumber );
                string timestamp   = TimeUtil.GetTimestamp();
                return ( transaction                             +
                         Seperator                               +
                         timestamp                               +
                         Seperator                               +
                         BuildIOSingleComandoMessage( device, Value )
                       );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public static string BuildSchedulerJobs( ref decimal transactionnumber, string device, string timesanddays )
            {
                string transaction = String.Format( FormatConstants.TransactionNumberFormat, transactionnumber );
                return ( transaction  +
                         Seperator    +
                         BuildSchedulerJobMessage( device, timesanddays )
                       );
            }

            // this is a inquiry of a IO state - when any endpoint receives this message it should response
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public static string WhatsUpWithYourDigitalIOs( string device )
            {
                return ( device + Seperator + MessageTyp.IO_WHATSUP ); 
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public static string GetCurrentSoftwareVersion( string device )
            {
                return ( device + Seperator + MessageTyp.ASK_FORVERSION );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public static string GetCurrentSchedulerJobStatus( string job, decimal transactionnumber )
            {
                string transaction = String.Format( FormatConstants.TransactionNumberFormat, transactionnumber );
                return ( transaction + Seperator + MessageTyp.ASK_SCHEDULER_JOB_STATUS + Seperator + job );
            }

            // general purpose comandos
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public static string BuildComandoMessage( ref decimal transactionnumber, string command )
            {
                string transaction = String.Format( FormatConstants.TransactionNumberFormat, transactionnumber );

                return (  transaction          +
                          Seperator            +
                          MessageTyp.HACOMAND  + 
                          Seperator            +
                          command              
                       );
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public static string BuildComandoMessage( ref decimal transactionnumber, string section, string command )
            {
                 string transaction = String.Format( FormatConstants.TransactionNumberFormat, transactionnumber );

                 return (  transaction         +
                           Seperator           +
                           MessageTyp.HACOMAND + 
                           Seperator           +
                           section             +
                           Seperator           +
                           command                                 
                        );
            }
            #endregion
        }
        #endregion

        #region TELEGRAM_CONSTANTS
        static class UdpTelegram
        {
            public const int DelfaultExpectedArrayElementsSignalTelegram = 3;
        }
        #endregion


    }
}
