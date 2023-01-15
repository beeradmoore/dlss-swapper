using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


// Full implementaiton is found here, https://docs.microsoft.com/en-US/troubleshoot/windows/win32/get-information-authenticode-signed-executables

namespace DLSS_Swapper
{
    /*
    // To use
    uint contentType = 0;
    uint formatType = 0;
    uint ignored = 0;
    IntPtr context = IntPtr.Zero;
    IntPtr pIgnored = IntPtr.Zero;

    IntPtr cryptMsg = IntPtr.Zero;

    if (!Crypt32.CryptQueryObject(
        Crypt32.CERT_QUERY_OBJECT_FILE,
        path, //Marshal.StringToHGlobalUni(path),
        Crypt32.CERT_QUERY_CONTENT_FLAG_ALL,
        Crypt32.CERT_QUERY_FORMAT_FLAG_ALL,
        0,
        ref ignored,
        ref contentType,
        ref formatType,
        ref pIgnored,
        ref cryptMsg,
        ref context))
    {
        int error = Marshal.GetLastWin32Error();

        Console.WriteLine((new System.ComponentModel.Win32Exception(error)).Message);

    }

    //expecting '10'; CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED
    Console.WriteLine("Context Type: " + contentType);

    //Which implies this is set
    Console.WriteLine("Crypt Msg: " + cryptMsg.ToInt32());


    // Get signer information size.
    fResult = Crypt32.CryptMsgGetParam(context,
    CMSG_SIGNER_INFO_PARAM,
    0,
    NULL,
    &dwSignerInfo);
    if (!fResult)
    {
        _tprintf(_T("CryptMsgGetParam failed with %x\n"), GetLastError());
        __leave;
    }         
    */
    internal static class Crypt32
    {
        public const int CERT_QUERY_OBJECT_FILE = 1;

        public const int CERT_QUERY_CONTENT_CERT = 1;
        public const int CERT_QUERY_CONTENT_CTL = 2;
        public const int CERT_QUERY_CONTENT_CRL = 3;
        public const int CERT_QUERY_CONTENT_SERIALIZED_STORE = 4;
        public const int CERT_QUERY_CONTENT_SERIALIZED_CERT = 5;
        public const int CERT_QUERY_CONTENT_SERIALIZED_CTL = 6;
        public const int CERT_QUERY_CONTENT_SERIALIZED_CRL = 7;
        public const int CERT_QUERY_CONTENT_PKCS7_SIGNED = 8;
        public const int CERT_QUERY_CONTENT_PKCS7_UNSIGNED = 9;
        public const int CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED = 10;
        public const int CERT_QUERY_CONTENT_PKCS10 = 11;
        public const int CERT_QUERY_CONTENT_PFX = 12;
        public const int CERT_QUERY_CONTENT_CERT_PAIR = 13;

        public const int CERT_QUERY_CONTENT_FLAG_CERT = (1 << CERT_QUERY_CONTENT_CERT);
        public const int CERT_QUERY_CONTENT_FLAG_CTL = (1 << CERT_QUERY_CONTENT_CTL);
        public const int CERT_QUERY_CONTENT_FLAG_CRL = (1 << CERT_QUERY_CONTENT_CRL);
        public const int CERT_QUERY_CONTENT_FLAG_SERIALIZED_STORE = (1 << CERT_QUERY_CONTENT_SERIALIZED_STORE);
        public const int CERT_QUERY_CONTENT_FLAG_SERIALIZED_CERT = (1 << CERT_QUERY_CONTENT_SERIALIZED_CERT);
        public const int CERT_QUERY_CONTENT_FLAG_SERIALIZED_CTL = (1 << CERT_QUERY_CONTENT_SERIALIZED_CTL);
        public const int CERT_QUERY_CONTENT_FLAG_SERIALIZED_CRL = (1 << CERT_QUERY_CONTENT_SERIALIZED_CRL);
        public const int CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED = (1 << CERT_QUERY_CONTENT_PKCS7_SIGNED);
        public const int CERT_QUERY_CONTENT_FLAG_PKCS7_UNSIGNED = (1 << CERT_QUERY_CONTENT_PKCS7_UNSIGNED);
        public const int CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED = (1 << CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED);
        public const int CERT_QUERY_CONTENT_FLAG_PKCS10 = (1 << CERT_QUERY_CONTENT_PKCS10);
        public const int CERT_QUERY_CONTENT_FLAG_PFX = (1 << CERT_QUERY_CONTENT_PFX);
        public const int CERT_QUERY_CONTENT_FLAG_CERT_PAIR = (1 << CERT_QUERY_CONTENT_CERT_PAIR);

        public const int CERT_QUERY_CONTENT_FLAG_ALL = CERT_QUERY_CONTENT_FLAG_CERT |
            CERT_QUERY_CONTENT_FLAG_CTL |
            CERT_QUERY_CONTENT_FLAG_CRL |
            CERT_QUERY_CONTENT_FLAG_SERIALIZED_STORE |
            CERT_QUERY_CONTENT_FLAG_SERIALIZED_CERT |
            CERT_QUERY_CONTENT_FLAG_SERIALIZED_CTL |
            CERT_QUERY_CONTENT_FLAG_SERIALIZED_CRL |
            CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED |
            CERT_QUERY_CONTENT_FLAG_PKCS7_UNSIGNED |
            CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED |
            CERT_QUERY_CONTENT_FLAG_PKCS10 |
            CERT_QUERY_CONTENT_FLAG_PFX |
            CERT_QUERY_CONTENT_FLAG_CERT_PAIR;


        public const int CERT_QUERY_FORMAT_BINARY = 1;
        public const int CERT_QUERY_FORMAT_BASE64_ENCODED = 2;
        public const int CERT_QUERY_FORMAT_ASN_ASCII_HEX_ENCODED = 3;

        public const int CERT_QUERY_FORMAT_FLAG_BINARY = (1 << CERT_QUERY_FORMAT_BINARY);
        public const int CERT_QUERY_FORMAT_FLAG_BASE64_ENCODED = (1 << CERT_QUERY_FORMAT_BASE64_ENCODED);
        public const int CERT_QUERY_FORMAT_FLAG_ASN_ASCII_HEX_ENCODED = (1 << CERT_QUERY_FORMAT_ASN_ASCII_HEX_ENCODED);

        public const int CERT_QUERY_FORMAT_FLAG_ALL = CERT_QUERY_FORMAT_FLAG_BINARY |
            CERT_QUERY_FORMAT_FLAG_BASE64_ENCODED |
            CERT_QUERY_FORMAT_FLAG_ASN_ASCII_HEX_ENCODED;

        [DllImport("CRYPT32.DLL", EntryPoint = "CryptQueryObject", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean CryptQueryObject(
            Int32 dwObjectType,
            [MarshalAs(UnmanagedType.LPWStr)] String pvObject,
            uint dwExpectedContentTypeFlags,
            uint dwExpectedFormatTypeFlags,
            uint dwFlags,
            ref uint pdwMsgAndCertEncodingType,
            ref uint pdwContentType,
            ref uint pdwFormatType,
            ref IntPtr phCertStore,
            ref IntPtr phMsg,
            ref IntPtr ppvContext
        );

        [DllImport("crypt32.dll", SetLastError = true)]
        public static extern bool CryptMsgGetParam(
             IntPtr hCryptMsg,
             uint dwParamType,
             uint dwIndex,
             IntPtr pvData,
             ref uint pcbData
        );

        [DllImport("crypt32.dll", SetLastError = true)]
        static extern IntPtr CertFindCertificateInStore(
            IntPtr hCertStore,
            uint dwCertEncodingType,
            uint dwFindFlags,
            uint dwFindType,
            IntPtr pszFindPara,
            IntPtr pPrevCertCntxt
            );
    }
}
