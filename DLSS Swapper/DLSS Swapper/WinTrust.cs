using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;



namespace DLSS_Swapper
{
    // Full implementaiton take from here, https://docs.microsoft.com/en-us/windows/win32/seccrypto/example-c-program--verifying-the-signature-of-a-pe-file
    // Help also from the comments in here, https://www.pinvoke.net/default.aspx/wintrust.winverifytrust

    internal static class WinTrust
    {
        //internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        // GUID of the action to perform
        internal const string WINTRUST_ACTION_GENERIC_VERIFY_V2 = "00AAC56B-CD44-11d0-8CC2-00C04FC295EE";

        internal enum WinVerifyTrustResult : uint
        {
            //    TRUST_E_NOSIGNATURE (when WTD_SAFER_FLAG is set in dwProvFlags)
            //      The subject isn't signed, has an invalid signature or unable
            //      to find the signer certificate. All signature verification
            //      errors will map to this error. Basically all errors except for
            //      publisher or timestamp certificate verification.
            //
            //      Call GetLastError() to get the underlying reason for not having
            //      a valid signature.
            //
            //      The following LastErrors indicate that the file doesn't have a
            //      signature: TRUST_E_NOSIGNATURE, TRUST_E_SUBJECT_FORM_UNKNOWN or
            //      TRUST_E_PROVIDER_UNKNOWN.
            //
            //      UI will never be displayed for this case.
            TRUST_E_NOSIGNATURE = 0x800B0100,

            // The form specified for the subject is not one supported or known by the specified trust provider.
            TRUST_E_SUBJECT_FORM_UNKNOWN = 0x800B0003,

            // Unknown trust provider.
            TRUST_E_PROVIDER_UNKNOWN = 0x800B0001,

            //    TRUST_E_EXPLICIT_DISTRUST
            //      Returned if the hash representing the subject is trusted as
            //      AUTHZLEVELID_DISALLOWED or the publisher is in the "Disallowed"
            //      store. Also returned if the publisher certificate is revoked.
            //
            //      UI will never be displayed for this case.
            TRUST_E_EXPLICIT_DISTRUST = 0x800B0111,

            //    ERROR_SUCCESS
            //      No UI unless noted below.
            //
            //      Returned for the following:
            //       - Hash representing the subject is trusted as
            //         AUTHZLEVELID_FULLYTRUSTED
            //       - The publisher certificate exists in the
            //         "TrustedPublisher" store and there weren't any verification errors.
            //       - UI was enabled and the user clicked "Yes" when asked
            //         to install and run the signed subject.
            //       - UI was disabled. No publisher or timestamp chain error.
            ERROR_SUCCESS = 0x0,

            //    TRUST_E_SUBJECT_NOT_TRUSTED
            //      UI was enabled and the the user clicked "No" when asked to install
            //      and run the signed subject.
            TRUST_E_SUBJECT_NOT_TRUSTED = 0x800B0004,

            //    CRYPT_E_SECURITY_SETTINGS
            //      The subject hash or publisher wasn't explicitly trusted and
            //      user trust wasn't allowed in the safer authenticode flags.
            //      No UI will be displayed for this case.
            //
            //      The subject is signed and its signature successfully
            //      verified.
            //
            //    Any publisher or timestamp chain error. If WTD_SAFER_FLAG wasn't set in
            //    dwProvFlags, any signed code verification error.
            //
            CRYPT_E_SECURITY_SETTINGS = 0x80092026,

            // An error occurred while reading or writing to a file.
            CRYPT_E_FILE_ERROR = 0x80092003,
        }

        [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern WinVerifyTrustResult WinVerifyTrust(
            [In] IntPtr hwnd,
            [In][MarshalAs(UnmanagedType.LPStruct)] Guid pgActionID,
            [In] WinTrustData pWVTData
        );




        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WinTrustFileInfo
        {
            public UInt32 cbStruct { get; private set; }                // = sizeof(WINTRUST_FILE_INFO)
            public IntPtr pcwszFilePath { get; private set; }           // required, file name to be verified
            public IntPtr hFile { get; private set; }                     // optional, open handle to FilePath
            public IntPtr pgKnownSubject { get; private set; }  // optional, subject type if it is known

            public WinTrustFileInfo(string filePath)
            {
                cbStruct = (UInt32)Marshal.SizeOf(typeof(WinTrustFileInfo));
                pcwszFilePath = Marshal.StringToCoTaskMemAuto(filePath);
                hFile = IntPtr.Zero;
                pgKnownSubject = IntPtr.Zero;
            }

            public void Dispose()
            {
                if (pcwszFilePath != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(pcwszFilePath);
                    pcwszFilePath = IntPtr.Zero;
                }
            }
        }

        internal enum WinTrustDataUIChoice : uint
        {
            All = 1,
            None = 2,
            NoBad = 3,
            NoGood = 4
        }

        internal enum WinTrustDataRevocationChecks : uint
        {
            None = 0x00000000,
            WholeChain = 0x00000001
        }

        internal enum WinTrustDataChoice : uint
        {
            File = 1,
            Catalog = 2,
            Blob = 3,
            Signer = 4,
            Certificate = 5
        }

        internal enum WinTrustDataStateAction : uint
        {
            Ignore = 0x00000000,
            Verify = 0x00000001,
            Close = 0x00000002,
            AutoCache = 0x00000003,
            AutoCacheFlush = 0x00000004
        }

        [FlagsAttribute]
        internal enum WinTrustDataProvFlags : uint
        {
            ProvFlagsMask = 0x0000FFFF,
            UseIe4TrustFlag = 0x00000001,
            NoIe4ChainFlag = 0x00000002,
            NoPolicyUsageFlag = 0x00000004,
            RevocationCheckNone = 0x00000010,
            RevocationCheckEndCert = 0x00000020,
            RevocationCheckChain = 0x00000040,
            RevocationCheckChainExcludeRoot = 0x00000080,
            SaferFlag = 0x00000100,
            HashOnlyFlag = 0x00000200,
            UseDefaultOsverCheck = 0x00000400,
            LifetimeSigningFlag = 0x00000800,
            CacheOnlyUrlRetrieval = 0x00001000, // affects CRL retrieval and AIA retrieval
            DisableMD2andMD4 = 0x00002000,
            MarkOfTheWeb = 0x00004000, // Mark-Of-The-Web
            CodeIntegrityDriverMode = 0x00008000,// Code Integrity driver mode
        }

        internal enum WinTrustDataUIContext : uint
        {
            Execute = 0,
            Install = 1
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WinTrustData
        {
            public UInt32 cbStruct { get; set; }                                        // = sizeof(WINTRUST_DATA)
            public IntPtr pPolicyCallbackData { get; set; }                             // optional: used to pass data between the app and policy
            public IntPtr pSIPClientData { get; set; }                                  // optional: used to pass data between the app and SIP.
            public WinTrustDataUIChoice dwUIChoice { get; set; }                        // required: UI choice.
            public WinTrustDataRevocationChecks fdwRevocationChecks { get; set; }       // required: certificate revocation check options
            public WinTrustDataChoice dwUnionChoice { get; set; }                       // required: which structure is being passed in?
            public IntPtr pFile { get; set; }                                           // individual file
            // but what about pCatalog, pBlob, pSgnr, pCert?
            public WinTrustDataStateAction dwStateAction { get; set; }                  // optional (Catalog File Processing)
            public IntPtr hWVTStateData { get; set; }                                   // optional (Catalog File Processing)
            public string pwszURLReference { get; set; }                                // optional: (future) used to determine zone.
            public WinTrustDataProvFlags dwProvFlags { get; set; }
            public WinTrustDataUIContext dwUIContext { get; set; }

            // constructor for silent WinTrustDataChoice.File check
            public WinTrustData(WinTrustFileInfo fileInfo)
            {
                cbStruct = (UInt32)Marshal.SizeOf(typeof(WinTrustData));
                pPolicyCallbackData = IntPtr.Zero;
                pSIPClientData = IntPtr.Zero;
                dwUIChoice = WinTrustDataUIChoice.None;
                fdwRevocationChecks = WinTrustDataRevocationChecks.None;
                dwUnionChoice = WinTrustDataChoice.File;
                pFile = IntPtr.Zero;
                dwStateAction = WinTrustDataStateAction.Ignore;
                hWVTStateData = IntPtr.Zero;
                pwszURLReference = null;
                dwProvFlags = WinTrustDataProvFlags.RevocationCheckChainExcludeRoot;
                dwUIContext = WinTrustDataUIContext.Execute;

                // On Win7SP1+, don't allow MD2 or MD4 signatures
                if ((Environment.OSVersion.Version.Major > 6) ||
                    ((Environment.OSVersion.Version.Major == 6) && (Environment.OSVersion.Version.Minor > 1)) ||
                    ((Environment.OSVersion.Version.Major == 6) && (Environment.OSVersion.Version.Minor == 1) && !String.IsNullOrEmpty(Environment.OSVersion.ServicePack)))
                {
                    dwProvFlags |= WinTrustDataProvFlags.DisableMD2andMD4;
                }

                WinTrustFileInfo wtfiData = fileInfo;
                pFile = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(WinTrustFileInfo)));
                Marshal.StructureToPtr(wtfiData, pFile, false);
            }

            public void Dispose()
            {
                if (pFile != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(pFile);
                    pFile = IntPtr.Zero;
                }
            }
        }


        public static bool VerifyEmbeddedSignature(string fileName)
        {
            WinVerifyTrustResult lStatus;
            uint dwLastError;

            WinTrustFileInfo FileData;
            WinTrustData WinTrustData;

            var validSignature = false;
            try
            {
                // Initialize the WINTRUST_FILE_INFO structure.
                FileData = new WinTrustFileInfo(fileName);

                /*
                WVTPolicyGUID specifies the policy to apply on the file
                WINTRUST_ACTION_GENERIC_VERIFY_V2 policy checks:

                1) The certificate used to sign the file chains up to a root 
                certificate located in the trusted root certificate store. This 
                implies that the identity of the publisher has been verified by 
                a certification authority.

                2) In cases where user interface is displayed (which this example
                does not do), WinVerifyTrust will check for whether the  
                end entity certificate is stored in the trusted publisher store,  
                implying that the user trusts content from this publisher.

                3) The end entity certificate has sufficient permission to sign 
                code, as indicated by the presence of a code signing EKU or no 
                EKU.
                */

                var WVTPolicyGUID = new Guid(WINTRUST_ACTION_GENERIC_VERIFY_V2);

                // Initialize the WinVerifyTrust input data structure.


                // Default all fields to 0.
                ///memset(&WinTrustData, 0, sizeof(WinTrustData));
                WinTrustData = new WinTrustData(FileData)
                {
                    cbStruct = (UInt32)Marshal.SizeOf(typeof(WinTrustData)),

                    // Use default code signing EKU.
                    pPolicyCallbackData = IntPtr.Zero,

                    // No data to pass to SIP.
                    pSIPClientData = IntPtr.Zero,

                    // Disable WVT UI.
                    dwUIChoice = WinTrustDataUIChoice.None,

                    // No revocation checking.
                    fdwRevocationChecks = WinTrustDataRevocationChecks.None,

                    // Verify an embedded signature on a file.
                    dwUnionChoice = WinTrustDataChoice.File,

                    // Verify action.
                    dwStateAction = WinTrustDataStateAction.Verify,

                    // Verification sets this value.
                    hWVTStateData = IntPtr.Zero,

                    // Not used.
                    pwszURLReference = null,

                    // This is not applicable if there is no UI because it changes 
                    // the UI to accommodate running applications instead of 
                    // installing applications.
                    dwUIContext = 0,

                    // Set pFile.
                    //pFile = &fileData; // done in the constructor
                };



                // WinVerifyTrust verifies signatures as specified by the GUID 
                // and Wintrust_Data.
                lStatus = WinVerifyTrust(IntPtr.Zero, WVTPolicyGUID, WinTrustData);


                switch (lStatus)
                {
                    case WinVerifyTrustResult.ERROR_SUCCESS:
                        /*
                        Signed file:
                            - Hash that represents the subject is trusted.

                            - Trusted publisher without any verification errors.

                            - UI was disabled in dwUIChoice. No publisher or 
                                time stamp chain errors.

                            - UI was enabled in dwUIChoice and the user clicked 
                                "Yes" when asked to install and run the signed 
                                subject.
                        */
                        validSignature = true;
                        System.Diagnostics.Debug.WriteLine("The file \"%s\" is signed and the signature was verified.", fileName);
                        break;

                    case WinVerifyTrustResult.TRUST_E_NOSIGNATURE:
                        // The file was not signed or had a signature 
                        // that was not valid.

                        // Get the reason for no signature.
                        dwLastError = (uint)Marshal.GetLastWin32Error();
                        if ((uint)WinVerifyTrustResult.TRUST_E_NOSIGNATURE == dwLastError ||
                            (uint)WinVerifyTrustResult.TRUST_E_SUBJECT_FORM_UNKNOWN == dwLastError ||
                            (uint)WinVerifyTrustResult.TRUST_E_PROVIDER_UNKNOWN == dwLastError)
                        {
                            // The file was not signed.
                            System.Diagnostics.Debug.WriteLine("The file \"%s\" is not signed.", fileName);
                        }
                        else
                        {
                            // The signature was not valid or there was an error 
                            // opening the file.
                            System.Diagnostics.Debug.WriteLine("An unknown error occurred trying to verify the signature of the \"%s\" file.", fileName);
                        }

                        break;

                    case WinVerifyTrustResult.TRUST_E_EXPLICIT_DISTRUST:
                        // The hash that represents the subject or the publisher 
                        // is not allowed by the admin or user.
                        System.Diagnostics.Debug.WriteLine("The signature is present, but specifically disallowed.");
                        break;

                    case WinVerifyTrustResult.TRUST_E_SUBJECT_NOT_TRUSTED:
                        // The user clicked "No" when asked to install and run.
                        System.Diagnostics.Debug.WriteLine("The signature is present, but not trusted.");
                        break;

                    case WinVerifyTrustResult.CRYPT_E_SECURITY_SETTINGS:
                        /*
                        The hash that represents the subject or the publisher 
                        was not explicitly trusted by the admin and the 
                        admin policy has disabled user trust. No signature, 
                        publisher or time stamp errors.
                        */
                        System.Diagnostics.Debug.WriteLine("CRYPT_E_SECURITY_SETTINGS - The hash representing the subject or the publisher wasn't explicitly trusted by the admin and admin policy has disabled user trust. No signature, publisher or timestamp errors.");
                        break;

                    case WinVerifyTrustResult.CRYPT_E_FILE_ERROR:
                        //An error occurred while reading or writing to a file.
                        System.Diagnostics.Debug.WriteLine("CRYPT_E_FILE_ERROR - An error occurred while reading or writing to a file.");
                        break;

                    default:
                        // The UI was disabled in dwUIChoice or the admin policy 
                        // has disabled user trust. lStatus contains the 
                        // publisher or time stamp chain error.
                        System.Diagnostics.Debug.WriteLine("Error is: 0x%x.\n", lStatus);
                        break;
                }

                // Any hWVTStateData must be released by a call with close.
                WinTrustData.dwStateAction = WinTrustDataStateAction.Close;

                lStatus = WinVerifyTrust(IntPtr.Zero, WVTPolicyGUID, WinTrustData);
            }
            catch (Exception err)
            {
                System.Diagnostics.Debug.WriteLine($"VerifyEmbeddedSignature Error: {err.Message}");

            }
            finally
            {
                //FileData.Dispose();
                //WinTrustData.Dispose();

                /*
                if (FileData != null)
                {
                    FileData.Dispose();
                    //winTrustFileInfo = null;
                }

                if (WinTrustData != null)
                {
                    WinTrustData.Dispose();
                    //WinTrustData = null;
                }
                */
            }

            return validSignature;
        }


        /*
        internal static bool VerifyEmbeddedSignature_Original(string fileName)
        {
            WinTrustFileInfo winTrustFileInfo = null;
            WinTrustData winTrustData = null;

            try
            {
                winTrustFileInfo = new WinTrust.WinTrustFileInfo(fileName);
                winTrustData = new WinTrustData(winTrustFileInfo);
                var guidAction = new Guid(WinTrust.WINTRUST_ACTION_GENERIC_VERIFY_V2);
                var result = WinTrust.WinVerifyTrust(WinTrust.INVALID_HANDLE_VALUE, guidAction, winTrustData);
                return result == WinVerifyTrustResult.Success;
            }
            catch (Exception err)
            {
                System.Diagnostics.Debug.WriteLine($"VerifyEmbeddedSignature Error: {err.Message}");
                return false;
            }
            finally
            {
                if (winTrustFileInfo != null)
                {
                    winTrustFileInfo.Dispose();
                    winTrustFileInfo = null;
                }

                if (winTrustData != null)
                {
                    winTrustData.Dispose();
                    winTrustData = null;
                }
            }
        }
        */
    }
}
