﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.EnterpriseServices;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;
using SharpShell.Attributes;
using SharpShell.Extensions;
using SharpShell.Interop;
using SharpShell.Pidl;
using SharpShell.ServerRegistration;

namespace SharpShell.SharpNamespaceExtension
{
    //  More info:
    //      Virtual Junction Points: http://msdn.microsoft.com/en-us/library/windows/desktop/cc144096(v=vs.85).aspx

    /// <summary>
    /// A <see cref="SharpNamespaceExtension"/> is a SharpShell implemented Shell Namespace Extension.
    /// This is the base class for all Shell Namespace Extensions.
    /// </summary>
    [ServerType(ServerType.ShellNamespaceExtension)]
    public abstract class SharpNamespaceExtension : 
        SharpShellServer, 
        IPersistFolder2,
        IShellFolder2,
        IShellNamespaceFolder

    {
        protected SharpNamespaceExtension()
        {
            Log("Instatiated Namespace Extension");
        }

        #region Implementation of IPersistFolder2.

        /// <summary>
        /// Retrieves the class identifier (CLSID) of the object.
        /// </summary>
        /// <param name="pClassID">A pointer to the location that receives the CLSID on return.
        /// The CLSID is a globally unique identifier (GUID) that uniquely represents an object 
        /// class that defines the code that can manipulate the object's data.</param>
        /// <returns>
        /// If the method succeeds, the return value is S_OK. Otherwise, it is E_FAIL.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        int IPersist.GetClassID(out Guid pClassID)
        {
            //  In our case, we can provide our SharpShell server class ID.
            pClassID = ServerClsid;

            //  We're done.
            return WinError.S_OK;
        }
        int IPersistFolder.GetClassID(out Guid pClassId) {return ((IPersist)this).GetClassID(out pClassId); }
        int IPersistFolder2.GetClassID(out Guid pClassId) { return ((IPersist)this).GetClassID(out pClassId); }

        /// <summary>
        /// Instructs a Shell folder object to initialize itself based on the information passed.
        /// </summary>
        /// <param name="pidl">The address of the ITEMIDLIST (item identifier list) structure 
        /// that specifies the absolute location of the folder.</param>
        /// <returns>
        /// If this method succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.
        /// </returns>
        int IPersistFolder.Initialize(IntPtr pidl)
        {
            //  The shell has initialised the extension and provided an absolute PIDL
            //  from the root (desktop) to the extension folder. We can store this
            //  pidl in our own format.
            extensionAbsolutePidl = PidlManager.PidlToIdlist(pidl);

            //  We're good, we've got the ID list.
            return WinError.S_OK;
        }
        int IPersistFolder2.Initialize(IntPtr pidl) { return ((IPersistFolder)this).Initialize(pidl); }

        /// <summary>
        /// Gets the ITEMIDLIST for the folder object.
        /// </summary>
        /// <param name="ppidl">The address of an ITEMIDLIST pointer. This PIDL represents the absolute location of the folder and must be relative to the desktop. This is typically a copy of the PIDL passed to Initialize.</param>
        /// <returns>
        /// If this method succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.
        /// </returns>
        /// <remarks>
        /// If the folder object has not been initialized, this method returns S_FALSE and ppidl is set to NULL.
        /// </remarks>
        int IPersistFolder2.GetCurFolder(out IntPtr ppidl)
        {
            //  If we haven't been initialised set a null pidl and return false.
            if (this.extensionAbsolutePidl == null)
            {
                ppidl = IntPtr.Zero;
                return WinError.S_FALSE;
            }

            //  Otherwise, set the pidl and return.
            ppidl = PidlManager.IdListToPidl(extensionAbsolutePidl);
            return WinError.S_OK;
        }

        #endregion

        #region Implmentation of IShellFolder

        /// <summary>
        /// Translates the display name of a file object or a folder into an item identifier list.
        /// </summary>
        /// <param name="hwnd">A window handle. The client should provide a window handle if it displays a dialog or message box. Otherwise set hwnd to NULL.</param>
        /// <param name="pbc">Optional. A pointer to a bind context used to pass parameters as inputs and outputs to the parsing function.</param>
        /// <param name="pszDisplayName">A null-terminated Unicode string with the display name.</param>
        /// <param name="pchEaten">A pointer to a ULONG value that receives the number of characters of the display name that was parsed. If your application does not need this information, set pchEaten to NULL, and no value will be returned.</param>
        /// <param name="ppidl">When this method returns, contains a pointer to the PIDL for the object.</param>
        /// <param name="pdwAttributes">The value used to query for file attributes. If not used, it should be set to NULL.</param>
        /// <returns>
        /// If this method succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        int IShellFolder.ParseDisplayName(IntPtr hwnd, IntPtr pbc, string pszDisplayName, ref uint pchEaten, out IntPtr ppidl, ref SFGAO pdwAttributes)
        {
            //  Use the ShellFolderImpl to handle the details.
            return ShellFolderImpl.ParseDisplayName(this, hwnd, pbc, pszDisplayName, ref pchEaten, out ppidl,
                ref pdwAttributes);
        }
        
        /// <summary>
        /// Allows a client to determine the contents of a folder by creating an item identifier enumeration object and returning its IEnumIDList interface.
        /// Return value: error code, if any
        /// </summary>
        /// <param name="hwnd">If user input is required to perform the enumeration, this window handle should be used by the enumeration object as the parent window to take user input.</param>
        /// <param name="grfFlags">Flags indicating which items to include in the  enumeration. For a list of possible values, see the SHCONTF enum.</param>
        /// <param name="ppenumIDList">Address that receives a pointer to the IEnumIDList interface of the enumeration object created by this method.</param>
        /// <returns>
        /// If this method succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        int IShellFolder.EnumObjects(IntPtr hwnd, SHCONTF grfFlags, out IEnumIDList ppenumIDList)
        {
            //  Use the ShellFolderImpl to handle the details.
            return ShellFolderImpl.EnumObjects(this, hwnd, grfFlags, out ppenumIDList);
        }

        /// <summary>
        /// Retrieves an IShellFolder object for a subfolder.
        //  Return value: error code, if any
        /// </summary>
        /// <param name="pidl">Address of an ITEMIDLIST structure (PIDL) that identifies the subfolder.</param>
        /// <param name="pbc">Optional address of an IBindCtx interface on a bind context object to be used during this operation.</param>
        /// <param name="riid">Identifier of the interface to return. </param>
        /// <param name="ppv">Address that receives the interface pointer.</param>
        /// <returns>If this method succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.</returns>
        int IShellFolder.BindToObject(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv)
        {
            //  Use the ShellFolderImpl to handle the details.
            return ShellFolderImpl.BindToObject(this, pidl, pbc, ref riid, out ppv);
        }

        /// <summary>
        /// Requests a pointer to an object's storage interface.
        /// Return value: error code, if any
        /// </summary>
        /// <param name="pidl">Address of an ITEMIDLIST structure that identifies the subfolder relative to its parent folder.</param>
        /// <param name="pbc">Optional address of an IBindCtx interface on a bind context object to be  used during this operation.</param>
        /// <param name="riid">Interface identifier (IID) of the requested storage interface.</param>
        /// <param name="ppv">Address that receives the interface pointer specified by riid.</param>
        /// <returns>
        /// If this method succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.
        /// </returns>
        int IShellFolder.BindToStorage(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv)
        {
            //  Use the ShellFolderImpl to handle the details.
            return ShellFolderImpl.BindToStorage(this, pidl, pbc, ref riid, out ppv);
        }

        /// <summary>
        /// Determines the relative order of two file objects or folders, given
        /// their item identifier lists. Return value: If this method is
        /// successful, the CODE field of the HRESULT contains one of the
        /// following values (the code can be retrived using the helper function
        /// GetHResultCode): Negative A negative return value indicates that the first item should precede the second (pidl1 &lt; pidl2).
        /// Positive A positive return value indicates that the first item should
        /// follow the second (pidl1 &gt; pidl2).  Zero A return value of zero
        /// indicates that the two items are the same (pidl1 = pidl2).
        /// </summary>
        /// <param name="lParam">Value that specifies how the comparison  should be performed. The lower Sixteen bits of lParam define the sorting  rule.
        /// The upper sixteen bits of lParam are used for flags that modify the sorting rule. values can be from  the SHCIDS enum</param>
        /// <param name="pidl1">Pointer to the first item's ITEMIDLIST structure.</param>
        /// <param name="pidl2">Pointer to the second item's ITEMIDLIST structure.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        int IShellFolder.CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2)
        {
            //  Use the ShellFolderImpl to handle the details.
            return ShellFolderImpl.CompareIDs(this, lParam, pidl1, pidl2);
        }

        /// <summary>
        /// Requests an object that can be used to obtain information from or interact
        /// with a folder object.
        /// Return value: error code, if any
        /// </summary>
        /// <param name="hwndOwner">Handle to the owner window.</param>
        /// <param name="riid">Identifier of the requested interface.</param>
        /// <param name="ppv">Address of a pointer to the requested interface.</param>
        /// <returns>
        /// If this method succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        int IShellFolder.CreateViewObject(IntPtr hwndOwner, ref Guid riid, out IntPtr ppv)
        {
            //  Use the ShellFolderImpl to handle the details.
            return ShellFolderImpl.CreateViewObject(this, this, hwndOwner, ref riid, out ppv);
        }

        /// <summary>
        /// Retrieves the attributes of one or more file objects or subfolders.
        /// Return value: error code, if any
        /// </summary>
        /// <param name="cidl">Number of file objects from which to retrieve attributes.</param>
        /// <param name="apidl">Address of an array of pointers to ITEMIDLIST structures, each of which  uniquely identifies a file object relative to the parent folder.</param>
        /// <param name="rgfInOut">Address of a single ULONG value that, on entry contains the attributes that the caller is
        /// requesting. On exit, this value contains the requested attributes that are common to all of the specified objects. this value can be from the SFGAO enum</param>
        /// <returns>
        /// If this method succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        int IShellFolder.GetAttributesOf(uint cidl, IntPtr[] apidl, ref SFGAO rgfInOut)
        {
            //  Use the ShellFolderImpl to handle the details.
            return ShellFolderImpl.GetAttributesOf(this, cidl, apidl, ref rgfInOut);
        }

        /// <summary>
        /// Retrieves an OLE interface that can be used to carry out actions on the
        /// specified file objects or folders. Return value: error code, if any
        /// </summary>
        /// <param name="hwndOwner">Handle to the owner window that the client should specify if it displays a dialog box or message box.</param>
        /// <param name="cidl">Number of file objects or subfolders specified in the apidl parameter.</param>
        /// <param name="apidl">Address of an array of pointers to ITEMIDLIST  structures, each of which  uniquely identifies a file object or subfolder relative to the parent folder.</param>
        /// <param name="riid">Identifier of the COM interface object to return.</param>
        /// <param name="rgfReserved">Reserved.</param>
        /// <param name="ppv">Pointer to the requested interface.</param>
        /// <returns>
        /// If this method succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.
        /// </returns>
        int IShellFolder.GetUIObjectOf(IntPtr hwndOwner, uint cidl, IntPtr[] apidl, ref Guid riid, uint rgfReserved, out IntPtr ppv)
        {
            //  Use the ShellFolderImpl to handle the details.
            return ShellFolderImpl.GetUIObjectOf(this, hwndOwner, cidl, apidl, ref riid, rgfReserved, out ppv);
        }

        /// <summary>
        /// Retrieves the display name for the specified file object or subfolder.
        /// Return value: error code, if any
        /// </summary>
        /// <param name="pidl">Address of an ITEMIDLIST structure (PIDL)  that uniquely identifies the file  object or subfolder relative to the parent  folder.</param>
        /// <param name="uFlags">Flags used to request the type of display name to return. For a list of possible values.</param>
        /// <param name="pName">Address of a STRRET structure in which to return the display name.</param>
        /// <returns>
        /// If this method succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.
        /// </returns>
        int IShellFolder.GetDisplayNameOf(IntPtr pidl, SHGDNF uFlags, out STRRET pName)
        {
            //  Use the ShellFolderImpl to handle the details.
            return ShellFolderImpl.GetDisplayNameOf(this, pidl, uFlags, out pName);
        }

        /// <summary>
        /// Sets the display name of a file object or subfolder, changing the item
        /// identifier in the process.
        /// Return value: error code, if any
        /// </summary>
        /// <param name="hwnd">Handle to the owner window of any dialog or message boxes that the client displays.</param>
        /// <param name="pidl">Pointer to an ITEMIDLIST structure that uniquely identifies the file object or subfolder relative to the parent folder.</param>
        /// <param name="pszName">Pointer to a null-terminated string that specifies the new display name.</param>
        /// <param name="uFlags">Flags indicating the type of name specified by  the lpszName parameter. For a list of possible values, see the description of the SHGNO enum.</param>
        /// <param name="ppidlOut"></param>
        /// <returns>
        /// If this method succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        int IShellFolder.SetNameOf(IntPtr hwnd, IntPtr pidl, string pszName, SHGDNF uFlags, out IntPtr ppidlOut)
        {
            //  Use the ShellFolderImpl to handle the details.
            return ShellFolderImpl.SetNameOf(this, hwnd, pidl, pszName, uFlags, out ppidlOut);
        }
        
        #endregion

        #region IShellFolder2 Implementation

        int IShellFolder2.ParseDisplayName(IntPtr hwnd, IntPtr pbc, string pszDisplayName, ref uint pchEaten,
            out IntPtr ppidl, ref SFGAO pdwAttributes)
        {
            return ((IShellFolder)this).ParseDisplayName(hwnd, pbc, pszDisplayName, pchEaten, out ppidl,
                ref pdwAttributes);
        }

        int IShellFolder2.EnumObjects(IntPtr hwnd, SHCONTF grfFlags, out IEnumIDList ppenumIDList)
        {
            return ((IShellFolder)this).EnumObjects(hwnd, grfFlags, out ppenumIDList);

        }

        int IShellFolder2.BindToObject(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv)
        {
            return ((IShellFolder)this).BindToObject(pidl, pbc, ref riid, out ppv);
        }

        int IShellFolder2.BindToStorage(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv)
        {
            return ((IShellFolder)this).BindToStorage(pidl, pbc, ref riid, out ppv);
        }

        int IShellFolder2.CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2)
        {
            return ((IShellFolder)this).CompareIDs(lParam, pidl1, pidl2);
        }

        int IShellFolder2.CreateViewObject(IntPtr hwndOwner, ref Guid riid, out IntPtr ppv)
        {
            return ((IShellFolder)this).CreateViewObject(hwndOwner, ref riid, out ppv);
        }

        int IShellFolder2.GetAttributesOf(uint cidl, IntPtr[] apidl, ref SFGAO rgfInOut)
        {
            return ((IShellFolder)this).GetAttributesOf(cidl, apidl, ref rgfInOut);
        }

        int IShellFolder2.GetUIObjectOf(IntPtr hwndOwner, uint cidl, IntPtr[] apidl, ref Guid riid, uint rgfReserved,
            out IntPtr ppv)
        {
            return ((IShellFolder)this).GetUIObjectOf(hwndOwner, cidl, apidl, ref riid, rgfReserved, out ppv);
        }

        int IShellFolder2.GetDisplayNameOf(IntPtr pidl, SHGDNF uFlags, out STRRET pName)
        {
            return ((IShellFolder)this).GetDisplayNameOf(pidl, uFlags, out pName);
        }

        int IShellFolder2.SetNameOf(IntPtr hwnd, IntPtr pidl, string pszName, SHGDNF uFlags, out IntPtr ppidlOut)
        {
            return ((IShellFolder)this).SetNameOf(hwnd, pidl, pszName, uFlags, out ppidlOut);
        }
        
        int IShellFolder2.GetDefaultSearchGUID(out Guid pguid)
        {
            //  Use the ShellFolderImpl to handle the details.
            return ShellFolderImpl.GetDefaultSearchGUID(this, out pguid);
        }

        int IShellFolder2.EnumSearches(out IEnumExtraSearch ppenum)
        {
            //  Use the ShellFolderImpl to handle the details.
            return ShellFolderImpl.EnumSearches(this, out ppenum);
        }

        int IShellFolder2.GetDefaultColumn(uint dwRes, out uint pSort, out uint pDisplay)
        {
            //  Use the ShellFolderImpl to handle the details.
            return ShellFolderImpl.GetDefaultColumn(this, dwRes, out pSort, out pDisplay);
        }

        int IShellFolder2.GetDefaultColumnState(uint iColumn, out SHCOLSTATEF pcsFlags)
        {
            //  Use the ShellFolderImpl to handle the details.
            return ShellFolderImpl.GetDefaultColumnState(this, iColumn, out pcsFlags);
        }

        int IShellFolder2.GetDetailsEx(IntPtr pidl, SHCOLUMNID pscid, out object pv)
        {
            //  Use the ShellFolderImpl to handle the details.
            return ShellFolderImpl.GetDetailsEx(this, pidl, pscid, out pv);
        }

        int IShellFolder2.GetDetailsOf(IntPtr pidl, uint iColumn, out IntPtr psd)
        {
            //  Use the ShellFolderImpl to handle the details.
            return ShellFolderImpl.GetDetailsOf(this, pidl, iColumn, out psd);
        }

        int IShellFolder2.MapColumnToSCID(uint iColumn, out SHCOLUMNID pscid)
        {
            //  Use the ShellFolderImpl to handle the details.
            return ShellFolderImpl.MapColumnToSCID(this, iColumn, out pscid);
        }

        #endregion

        #region Custom Registration and Unregistration

        /// <summary>
        /// The custom registration function.
        /// </summary>
        /// <param name="serverType">Type of the server.</param>
        /// <param name="registrationType">Type of the registration.</param>
        [CustomRegisterFunction]
        internal static void CustomRegisterFunction(Type serverType, RegistrationType registrationType)
        {
            //  TODO: currently, we will only support virtual junction points.

            //  Get the junction point.
            var junctionPoint = NamespaceExtensionJunctionPointAttribute.GetJunctionPoint(serverType);

            //  If the junction point is not defined, we must fail.
            if (junctionPoint == null)
                throw new InvalidOperationException("Unable to register a SharpNamespaceExtension as it is missing it's junction point definition.");

            //  Now we have the junction point, we can build the key as below:
            /* HKEY_LOCAL_MACHINE or HKEY_CURRENT_USER
                   Software
                      Microsoft
                         Windows
                            CurrentVersion
                               Explorer
                                  Virtual Folder Name
                                     NameSpace
                                        {Extension CLSID}
                                           (Default) = Junction Point Name
            */

            //  Work out the hive and view to use, based on the junction point availability
            //  and the registration mode.
            var hive = junctionPoint.Availablity == NamespaceExtensionAvailability.CurrentUser
                ? RegistryHive.CurrentUser
                : RegistryHive.LocalMachine;
            var view = registrationType == RegistrationType.OS64Bit ? RegistryView.Registry64 : RegistryView.Registry32;

            //  Now open the base key.
            using (var baseKey = RegistryKey.OpenBaseKey(hive, view))
            {
                //  Create the path to the virtual folder namespace key.
                var virtualFolderNamespacePath =
                    string.Format(@"Software\Microsoft\Windows\CurrentVersion\Explorer\{0}\NameSpace",
                        RegistryKeyAttribute.GetRegistryKey(junctionPoint.Location));

                //  Open the virtual folder namespace key,
                using (var namespaceKey = baseKey.OpenSubKey(virtualFolderNamespacePath,
                    RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.WriteKey))
                {
                    //  If we don't have the key, we've got a problem.
                    if (namespaceKey == null)
                        throw new InvalidOperationException("Cannot open the Virtual Folder NameSpace key.");

                    //  Write the server guid as a key, then the Junction Point Name as it's default value.
                    var serverKey = namespaceKey.CreateSubKey(serverType.GUID.ToRegistryString());
                    if(serverKey == null)
                        throw new InvalidOperationException("Failed to create the Virtual Folder NameSpace extension.");
                    serverKey.SetValue(null, junctionPoint.Name, RegistryValueKind.String);
                }
            }

            //  We can now customise the class registration as needed.
            //  The class is already registered by the Installation of the server, we're only
            //  adapting it here.

            //  Open the classes root.
            using (var classesBaseKey = registrationType == RegistrationType.OS64Bit
                ? RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64) :
                  RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry32))
            {
                //  Our server guid.
                var serverGuid = serverType.GUID.ToRegistryString();

                //  Open the Class Key.
                using (var classKey = classesBaseKey
                    .OpenSubKey(string.Format(@"CLSID\{0}", serverGuid),
                    RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.WriteKey))
                {
                    //  If we don't have the key, we've got a problem.
                    if (classKey == null)
                        throw new InvalidOperationException("Cannot open the class key.");

                    //  Create an instance of the server to get it's registration settings.
                    var serverInstance = (SharpNamespaceExtension)Activator.CreateInstance(serverType);
                    var registrationSettings = serverInstance.GetRegistrationSettings();

                    //  Apply basic settings.
                    if(registrationSettings.HideFolderVerbs) classKey.SetValue("HideFolderVerbs", 1, RegistryValueKind.DWord);

                    //  TODO: at some stage, we may handle WantsFORPARSING
                    //  TODO: at some stage, we must handle HideAsDelete
                    //  TODO: at some stage, we must handle HideAsDeletePerUser
                    //  TODO: at some stage, we must handle QueryForOverlay

                    //  The default value is the junction point name.
                    classKey.SetValue(null, junctionPoint.Name, RegistryValueKind.String);

                    //  Set the infotip. TODO
                    // classKey.SetValue(@"InfoTip", infoTip, RegistryValueKind.String);

                    //  Set the default icon. TODO key not attribute
                    //  classKey.SetValue(@"DefaultIcon", "File.dll,index", RegistryValueKind.String);
                    
                    //  TODO support custom verbs with a 'Shell' subkey.
                    //  TODO support custom shortcut menu handler with ShellEx.
                    //  TODO tie in support for a property sheet handler.
                    
                    //  Set the attributes.
                    using (var shellFolderKey = classKey.CreateSubKey("ShellFolder"))
                    {
                        if(shellFolderKey == null)
                            throw new InvalidOperationException("An exception occured creating the ShellFolder key.");
                        shellFolderKey.SetValue("Attributes", (int)registrationSettings.ExtensionAttributes, RegistryValueKind.DWord);
                    }
                    //  TODO Critical, as we don't set SGFAO_FOLDER in the above currently, we can't display child items.
                    //  See documentation at: http://msdn.microsoft.com/en-us/library/windows/desktop/cc144093.aspx#ishellfolder
                }
            }
        }

        /// <summary>
        /// Customs the unregister function.
        /// </summary>
        /// <param name="serverType">Type of the server.</param>
        /// <param name="registrationType">Type of the registration.</param>
        [CustomUnregisterFunction]
        internal static void CustomUnregisterFunction(Type serverType, RegistrationType registrationType)
        {
            //  Get the junction point.
            var junctionPoint = NamespaceExtensionJunctionPointAttribute.GetJunctionPoint(serverType);

            //  If the junction point is not defined, we must fail.
            if (junctionPoint == null)
                throw new InvalidOperationException("Unable to register a SharpNamespaceExtension as it is missing it's junction point definition.");
            
            //  Work out the hive and view to use, based on the junction point availability
            //  and the registration mode.
            var hive = junctionPoint.Availablity == NamespaceExtensionAvailability.CurrentUser
                ? RegistryHive.CurrentUser
                : RegistryHive.LocalMachine;
            var view = registrationType == RegistrationType.OS64Bit ? RegistryView.Registry64 : RegistryView.Registry32;

            //  Now open the base key.
            using (var baseKey = RegistryKey.OpenBaseKey(hive, view))
            {
                //  Create the path to the virtual folder namespace key.
                var virtualFolderNamespacePath =
                    string.Format(@"Software\Microsoft\Windows\CurrentVersion\Explorer\{0}\NameSpace",
                        RegistryKeyAttribute.GetRegistryKey(junctionPoint.Location));
                
                //  Open the virtual folder namespace key,
                using (var namespaceKey = baseKey.OpenSubKey(virtualFolderNamespacePath,
                    RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.WriteKey))
                {
                    //  If we don't have the key, we've got a problem.
                    if (namespaceKey == null)
                        throw new InvalidOperationException("Cannot open the Virtual Folder NameSpace key.");

                    //  Delete the shell extension key, which is just it's CLSID.
                    namespaceKey.DeleteSubKeyTree(serverType.GUID.ToRegistryString());
                }
            }
        }

        #endregion

        #region Implementation of IShellNamespaceItem and IShellNamespaceFolder

        ShellId IShellNamespaceItem.GetShellId()
        {
            throw new NotImplementedException();
        }

        string IShellNamespaceItem.GetDisplayName(DisplayNameContext displayNameContext)
        {
            //  TODO handle all cases in future, for now we can just use the attribute version
            return DisplayName;
        }

        AttributeFlags IShellNamespaceItem.GetAttributes()
        {
            return GetRegistrationSettings().ExtensionAttributes;
        }

        IEnumerable<IShellNamespaceItem> IShellNamespaceFolder.GetChildren(ShellNamespaceEnumerationFlags flags)
        {
            return GetChildren(flags);
        }

        ShellNamespaceFolderView IShellNamespaceFolder.GetView()
        {
            return GetView();
        }

        #endregion

        private IShellNamespaceItem GetChildItem(IdList idList)
        {
            var kids = GetChildren(ShellNamespaceEnumerationFlags.Folders | ShellNamespaceEnumerationFlags.Items);
            var item = kids.FirstOrDefault(ci => idList.Matches(ci.GetShellId()));
            return item;
        }

        /// <summary>
        /// Gets the registration settings. This function is called only during the initial
        /// registration of a shell namespace extension to provide core configuration.
        /// </summary>
        /// <returns>Registration settings for the server.</returns>
        public abstract NamespaceExtensionRegistrationSettings GetRegistrationSettings();

        /// <summary>
        /// Gets the children of the extension.
        /// </summary>
        /// <param name="flags">The flags. Only return children that match the flags.</param>
        /// <returns>The children of the extension.</returns>
        protected abstract IEnumerable<IShellNamespaceItem> GetChildren(ShellNamespaceEnumerationFlags flags);

        /// <summary>
        /// Gets the folder view for the extension.
        /// This can be a <see cref="DefaultNamespaceFolderView" /> which uses the standard user
        /// interface with customised columns, or a <see cref="CustomNamespaceFolderView"/> which presents
        /// a fully customised user inteface.
        /// </summary>
        /// <returns>The folder view for the extension.</returns>
        protected abstract ShellNamespaceFolderView GetView();

        /// <summary>
        /// The extension absolute pidl.
        /// </summary>
        private IdList extensionAbsolutePidl;
    }

    /// <summary>
    /// ShellNamespaceEnumerationFlags for an enumeration of shell items.
    /// </summary>
    [Flags]
    public enum ShellNamespaceEnumerationFlags
    {
        /// <summary>
        /// The enumeration must include folders.
        /// </summary>
        Folders = 1,

        /// <summary>
        /// The enumeration must include items.
        /// </summary>
        Items = 2
    }
}
