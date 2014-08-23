Portable.Data.Sqlite
====================

This is a portable cross-platform ADO provider for SQLite databases, featuring table-column-level and table-record-level data encryption.  It is intended to be a Portable Class Library (PCL) that does the following:

  1. Works properly on the following platforms:
    * Windows desktop (with .NET Framework 4.5 or higher)
    * Windows Runtime (a.k.a. Windows Store) 8 and higher
    * Windows Phone 8 and higher
    * Xamarin.iOS
    * Xamarin.Android
  2. Runs on top of (and requires) the Portable Class Library for SQLite (SQLitePCL) from MSOpenTech - info available [here](http://sqlitepcl.codeplex.com/) - NuGet package available [here](http://www.nuget.org/packages/SQLitePCL)
  3. Provides access to SQLite databases via the native built-into-the-operating-system implementations of SQLite for the platforms listed above (where an implementation exists).
  4. Provides a PCL-based ADO-style way of interacting with SQLite databases - based on a portable (PCL) implementation of Mono.Data.Sqlite available [here](https://github.com/mattleibow/Mono.Data.Sqlite)
  5. Enables **easy table record-level and column-level encryption of data** in your SQLite database.

The developer of this library welcomes all feedback, suggestions, issue/bug reports, and pull requests. Please log questions and issues in the Portable.Data.Sqlite GitHub *Issues* section - available [here](https://github.com/ellisnet/Portable.Data.Sqlite/issues)

Important Notes About Encryption
--------------------------------
  1. For various reasons, this library **does not include** an encryption algorithm.  All operating systems listed above have built-in AES encryption that can be used with this library (as an example of one type of encryption algorithm that works well).  It is up to you to specify the algorithm to use by implementing the *IObjectCryptEngine* interface.  This allows you to choose exactly how your data will be encrypted.  Taking a well understood encryption algorithm and implementing your own *encryption engine* should not be too difficult; see detailed information below.
  2. This library **does not implement full database encryption** - for that, please investigate SQLCipher - available [here](http://sqlcipher.net/)  
It is up to you - the developer who is using this library - to decide which data in the database to encrypt; and which data not to.  You can encrypt any column in any table, and you can encrypt an entire table (i.e. all of the records in a table).

The (Potentially) Difficult Parts Up Front:
-------------------------------------------
So let's just get straight to the potentially difficult parts.  They are:

  1. It may be difficult to set up SQLitePCL (i.e. the Portable Class Library for SQLite mentioned above) if you are developing for a platform that doesn't come with SQLite built-in - like Windows (desktop) or Windows Store.  You have to install an add-on for Visual Studio and/or download a .DLL or two.  But there is excellent information available on the SQLitePCL CodePlex site - [documentation here](https://sqlitepcl.codeplex.com/documentation)  
Note that there is also an extra step needed for Xamarin.iOS, where you initialize/load SQLitePCL.Ext.dll by calling *SQLitePCL.CurrentPlatform.Init()*
  2. You need to implement your chosen encryption algorithm, by creating a class that implements *Portable.Data.Sqlite.IObjectCryptEngine* - You will need to create a class that has EncryptObject() and DecryptObject&lt;T&gt;() methods.  EncryptObject() will take just about any CLR Object and will serialize it and encrypt it, and then return the byte-array as a string; DecryptObject&lt;T&gt;() will take a byte-array-as-a-string, decrypt it and de-serialize it back to an object of the type specified as &lt;T&gt;.  So, DecryptObject&lt;MyObject&gt;(myEncryptedString) should decrypt myEncryptedString and turn it into a MyObject-class object and return it.  I have found that using the popular JSON.NET library (from [here](http://www.newtonsoft.com) ) works great for the serializing and de-serializing part.  Also, if you are looking for a PCL-based library with a bunch of encryption algorithms, take a look at Bouncy Castle PCL (Portable.BouncyCastle) - available on NuGet [here](http://www.nuget.org/packages/Portable.BouncyCastle)

So, here is an example of how you *could* implement *IObjectCryptEngine* - using AES encryption that comes built into the OS on all supported platforms.  The following code should provide reasonable data encryption/security, but has a few issues as described in the comments:

```c#
using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Portable.Data.Sqlite;

//Example AES-based "crypt engine" for use with Portable.Data.Sqlite,
//  should work on all platforms supported by Portable.Data.Sqlite.

//Disclaimer:
//  THIS SAMPLE CODE IS BEING PROVIDED FOR DEMONSTRATION PURPOSES ONLY, AND
//  IS NOT INTENDED FOR USE WITH SOFTWARE THAT MUST PROVIDE ACTUAL DATA SECURITY.
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
//  SOFTWARE.

public class SimpleAesCryptEngine : IObjectCryptEngine {

    string _cryptoKey;
    Aes _aesProvider;

    private byte[] getBytes(string text, int requiredLength) {
        var result = new byte[requiredLength];
        byte[] textBytes = Encoding.UTF8.GetBytes(text);
        int offset = 0;
        while (offset < requiredLength) {
            int toCopy = (requiredLength >= (offset + textBytes.Length)) ?
                textBytes.Length : requiredLength - offset;
            Buffer.BlockCopy(textBytes, 0, result, offset, toCopy);
            offset += toCopy;
        }
        return result;
    }

    public SimpleAesCryptEngine(string cryptoKey) {
        _cryptoKey = cryptoKey;
        _aesProvider = Aes.Create();
        _aesProvider.Key = getBytes(cryptoKey, _aesProvider.Key.Length);
        //Here we are using the same value for all initialization vectors.
        //  This is NOT RECOMMENDED - it should be randomly generated;
        //  however, then you need a way to retrieve it for decryption.
        //  More info: http://en.wikipedia.org/wiki/Initialization_vector
        _aesProvider.IV = getBytes("THIS SHOULD BE RANDOM", _aesProvider.IV.Length);
    }

    public T DecryptObject<T>(string stringToDecrypt) {
        T result = default(T);
        if (stringToDecrypt != null) {
            byte[] bytesToDecrypt = Convert.FromBase64String(stringToDecrypt);
            byte[] decryptedBytes = 
                _aesProvider.CreateDecryptor().TransformFinalBlock(bytesToDecrypt, 0, bytesToDecrypt.Length);
            result = 
                JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(decryptedBytes));
        }
        return result;
    }

    public string EncryptObject(object objectToEncrypt) {
        string result = null;
        if (objectToEncrypt != null) {
            byte[] bytesToEncrypt = 
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(objectToEncrypt));
            //Not sure if I should be using TransformFinalBlock() here, 
            //  or if it is more secure if I break the byte array into
            //  blocks and process one block at a time.
            byte[] encryptedBytes = 
                _aesProvider.CreateEncryptor().TransformFinalBlock(bytesToEncrypt, 0, bytesToEncrypt.Length);
            result = Convert.ToBase64String(encryptedBytes);
        }
        return result;
    }

    public void Dispose() {
        _cryptoKey = null;
        _aesProvider.Dispose();
        _aesProvider = null;
    }
}
```

Examples of Using the Library Coming Soon...
--------------------------------------------
