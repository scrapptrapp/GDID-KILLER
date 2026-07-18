========================================
 GDID Guard - README
========================================

WHAT THIS IS
------------
GDID Guard is a small Windows tool that helps reduce a few specific
ways Windows can identify and report on your device:

1. GDID (Global Device Identifier) - a Microsoft-account-linked ID
   stored locally at:
   HKCU\SOFTWARE\Microsoft\IdentityCRL\ExtendedProperties (LID value)
   This only applies if you're signed into Windows with a Microsoft
   account. Local Windows accounts generally don't have this value
   at all.

2. Connected Devices Platform / DiagTrack - background telemetry
   services Windows uses to report device and usage data.

3. Delivery Optimization P2P sharing - a feature that can share your
   downloaded updates/apps with other PCs (and vice versa) over the
   internet.

HOW TO USE IT
-------------
1. Run GdidGuard.exe.
   - Windows will likely show a UAC (User Account Control) prompt
     asking to let it make changes. Click YES.
   - This is required because stopping/disabling Windows services
     needs administrator rights. If you click No, the app will still
     open, but the service-related toggles won't work.

2. Check the boxes for whatever you want to enable:
   [ ] Auto-wipe GDID every 60 seconds
   [ ] Disable Connected Devices Platform / DiagTrack
   [ ] Disable Delivery Optimization P2P sharing

3. Click "Apply Selected".

4. Watch the black log box at the bottom - it shows exactly what the
   app is doing in real time, with timestamps. Nothing happens
   silently; if something fails, it'll say so there.

5. Click "Mute Audio" / "Play Audio" to toggle the background music
   on or off. It loops automatically when the app opens.

6. Closing the window (the X button) does NOT quit the app - it
   minimizes to your system tray instead, so protection keeps
   running in the background. Right-click the tray icon to bring
   the window back or to fully Exit.

HOW TO VERIFY IT'S ACTUALLY WORKING
------------------------------------
You don't have to just take the app's word for it. Open a separate
PowerShell window and check before/after:

  Check GDID value:
    Get-ItemProperty -Path "HKCU:\SOFTWARE\Microsoft\IdentityCRL\ExtendedProperties" -Name "LID" -ErrorAction SilentlyContinue

  Check DiagTrack service status:
    Get-Service DiagTrack | Select Status, StartType

  Check Delivery Optimization setting:
    Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config" -Name "DODownloadMode" -ErrorAction SilentlyContinue

Run a command, click the matching checkbox + Apply in the app, then
run the same command again. You should see the value/service state
actually change.

WHAT THIS DOES NOT DO (BE REALISTIC)
-------------------------------------
- It cannot erase anything already recorded on Microsoft's own
  servers before you ran it. GDID in particular is understood to be
  a server-side association, not something purely local - deleting
  the local copy prevents it from being re-reported locally, but it
  is not a guaranteed way to erase history that already exists.
- It does not make Windows fully "untraceable." It reduces a few
  specific, known reporting mechanisms.
- If you use Xbox, Microsoft Store, OneDrive, or similar
  Microsoft-account features, some of them may stop working properly
  or ask you to sign in again after these changes.

REQUIREMENTS
-------------
- Windows 10 or 11, 64-bit
- Administrator rights (for the service toggles to work)

UNINSTALLING / UNDOING CHANGES
--------------------------------
- Re-enable DiagTrack:
    sc.exe config DiagTrack start= demand
    Start-Service DiagTrack

- Re-enable Connected Devices Platform:
    sc.exe config CDPSvc start= demand
    Start-Service CDPSvc

- Re-enable Delivery Optimization P2P sharing:
    Turn "Allow downloads from other PCs" back ON in
    Settings > Windows Update > Delivery Optimization

- To stop GDID Guard from running in the background, right-click its
  tray icon and choose Exit.

QUESTIONS / ISSUES
--------------------
This is a personal utility, not an official Microsoft or third-party
product. If something in it breaks or behaves unexpectedly, check
the log box first - it's designed to tell you exactly what happened.
