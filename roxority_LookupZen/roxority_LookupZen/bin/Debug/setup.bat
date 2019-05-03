@rem======================================================================
@rem
@rem    setup.bat
@rem
@rem======================================================================

@echo off
setlocal
pushd .

goto LInitialize


@rem----------------------------------------------------------------------
@rem    LInitialize
@rem----------------------------------------------------------------------
:LInitialize
    set SPLocation=%CommonProgramFiles%\Microsoft Shared\web server extensions\12
    set SPAdminTool=%SPLocation%\BIN\stsadm.exe
    set Install=
    set Uninstall=
    set PackageFile=%~dp0roxority_LookupZen.wsp
    set PackageName=roxority_LookupZen.wsp
    set DefaultWebUrl=http://localhost/sites/greenbox
    set DefaultSiteUrl=http://localhost/sites/greenbox
    set TargetWebUrl=
    set TargetSiteUrl=
    set SPTemplateLocation=%SPLocation%\template
    set SPFeaturesLocation=%SPTemplateLocation%\features
    set SPSiteTemplateLocation=%SPTemplateLocation%\sitetemplates
    set ValidationFailed=

    goto LParseArgs


@rem----------------------------------------------------------------------
@rem    LParseArgs
@rem----------------------------------------------------------------------
:LParseArgs
    @rem --- help ---
    if "%1" == "/?"    goto LHelp
    if "%1" == "-?"    goto LHelp
    if "%1" == "/h"    goto LHelp
    if "%1" == "-h"    goto LHelp
    if "%1" == "/help" goto LHelp
    if "%1" == "-help" goto LHelp

    @rem --- Fix execute task ---
    if "%1" == "/i"         (set Install=1)   & shift & goto LParseArgs
    if "%1" == "-i"         (set Install=1)   & shift & goto LParseArgs
    if "%1" == "/install"   (set Install=1)   & shift & goto LParseArgs
    if "%1" == "-install"   (set Install=1)   & shift & goto LParseArgs
    if "%1" == "/u"         (set Uninstall=1) & shift & goto LParseArgs
    if "%1" == "-u"         (set Uninstall=1) & shift & goto LParseArgs
    if "%1" == "/uninstall" (set Uninstall=1) & shift & goto LParseArgs
    if "%1" == "-uninstall" (set Uninstall=1) & shift & goto LParseArgs
    
    @rem --- Fix url ---
    if "%1" == "/weburl"  (set TargetWebUrl=%2)  & shift & shift & goto LParseArgs
    if "%1" == "-weburl"  (set TargetWebUrl=%2)  & shift & shift & goto LParseArgs
    if "%1" == "/siteurl" (set TargetSiteUrl=%2) & shift & shift & goto LParseArgs
    if "%1" == "-siteurl" (set TargetSiteUrl=%2) & shift & shift & goto LParseArgs

    @rem --- Check invalid arguments ---
    if not "%1" == "" (
        echo Invalid argument.
        goto LHelp
    )

    @rem --- Check arguments ---
    if "%Install%" == "1" (
        if "%Uninstall%" == "1" (
            goto LHelp
        )
    )

    if "%Install%" == "" (
        if "%Uninstall%" == "" (
            set Install=1
        )
    )

    if "%TargetSiteUrl%" == "" (
        if "%TargetWebUrl%" == "" (
            set TargetWebUrl=%DefaultWebUrl%
            set TargetSiteUrl=%DefaultSiteUrl%
        )
        if not "%TargetWebUrl%" == "" (
            set TargetSiteUrl=%TargetWebUrl%
            echo Setting TargetSiteUrl to be %TargetWebUrl%
        )
    )

    if "%TargetWebUrl%" == "" (
        set TargetWebUrl=%TargetSiteUrl%
        echo Setting TargetWebUrl to be %TargetSiteUrl%
    )

	goto LMain


@rem----------------------------------------------------------------------
@rem	LHelp
@rem----------------------------------------------------------------------
:LHelp
    echo Usage:
    echo setup.bat [/install or /uninstall][/weburl ^<url^>][/siteurl ^<url^>]
    echo           [/help]
    echo.
    echo Options:
    echo  /install or /uninstall
    echo  Install specified Solution package (.wsp) to the SharePoint server
    echo  or uninstall specified Solution from the SharePoint server.
    echo  Default value: install
    echo  /weburl
    echo  Specify a web url of the SharePoint server.
    echo  Default value: %DefaultWebUrl%
    echo  /siteurl
    echo  Specify a site url of the SharePoint server.
    echo  Default value: %DefaultSiteUrl%
    echo  /help
    echo  Show this information.
    echo.

	goto LTerminate


@rem----------------------------------------------------------------------
@rem    LMain
@rem----------------------------------------------------------------------
:LMain
	if "%Install%" == "1" (
      call :LValidate
  )
	if "%Install%" == "1" (
	   if not "%ValidationFailed%" == "1" (
        call :LDeploy
     )
  )
	if "%Uninstall%" == "1" (
      call :LRetract
  )

	goto LTerminate


@rem----------------------------------------------------------------------
@rem    LValidate
@rem----------------------------------------------------------------------
:LValidate
    echo Validating the content of solution %PackageName% ...
    echo.

    if exist "%SPTemplateLocation%\images\roxority_LookupZen\completeallwftasks.gif" (
       echo Error: Template file completeallwftasks.gif already exists in current SharePoint.
       set ValidationFailed=1
    )

    if exist "%SPTemplateLocation%\images\roxority_LookupZen\ewr052.gif" (
       echo Error: Template file ewr052.gif already exists in current SharePoint.
       set ValidationFailed=1
    )

    if exist "%SPTemplateLocation%\images\roxority_LookupZen\feature.png" (
       echo Error: Template file feature.png already exists in current SharePoint.
       set ValidationFailed=1
    )

    if exist "%SPTemplateLocation%\layouts\roxority_LookupZen\help\res\help.css" (
       echo Error: Template file help.css already exists in current SharePoint.
       set ValidationFailed=1
    )

    if exist "%SPTemplateLocation%\layouts\roxority_LookupZen\help\eula.html" (
       echo Error: Template file eula.html already exists in current SharePoint.
       set ValidationFailed=1
    )

    if exist "%SPTemplateLocation%\layouts\roxority_LookupZen\help\farm_site_config.html" (
       echo Error: Template file farm_site_config.html already exists in current SharePoint.
       set ValidationFailed=1
    )

    if exist "%SPTemplateLocation%\layouts\roxority_LookupZen\help\intro.html" (
       echo Error: Template file intro.html already exists in current SharePoint.
       set ValidationFailed=1
    )

    if exist "%SPTemplateLocation%\layouts\roxority_LookupZen\help\release_notes.html" (
       echo Error: Template file release_notes.html already exists in current SharePoint.
       set ValidationFailed=1
    )

    if exist "%SPTemplateLocation%\layouts\roxority_LookupZen\help\setup.html" (
       echo Error: Template file setup.html already exists in current SharePoint.
       set ValidationFailed=1
    )

    if exist "%SPTemplateLocation%\layouts\roxority_LookupZen\default.aspx" (
       echo Error: Template file default.aspx already exists in current SharePoint.
       set ValidationFailed=1
    )

    if exist "%SPTemplateLocation%\layouts\roxority_LookupZen\jQuery.js" (
       echo Error: Template file jQuery.js already exists in current SharePoint.
       set ValidationFailed=1
    )

    if exist "%SPTemplateLocation%\layouts\roxority_LookupZen\micro.rox" (
       echo Error: Template file micro.rox already exists in current SharePoint.
       set ValidationFailed=1
    )

    if exist "%SPTemplateLocation%\layouts\roxority_LookupZen\roxbase.js" (
       echo Error: Template file roxbase.js already exists in current SharePoint.
       set ValidationFailed=1
    )

    if exist "%SPTemplateLocation%\layouts\roxority_LookupZen\roxority.js" (
       echo Error: Template file roxority.js already exists in current SharePoint.
       set ValidationFailed=1
    )

    if exist "%SPTemplateLocation%\xml\fldtypes_roxority_LookupZen.xml" (
       echo Error: Template file fldtypes_roxority_LookupZen.xml already exists in current SharePoint.
       set ValidationFailed=1
    )

    goto :EOF


@rem----------------------------------------------------------------------
@rem    LDeploy
@rem----------------------------------------------------------------------
:LDeploy
    echo Adding solution %PackageName% to the SharePoint ...
    "%SPAdminTool%" -o addsolution -filename "%PackageFile%"

    echo Deploying solution %PackageName% ...
    "%SPAdminTool%" -o deploysolution -name "%PackageName%" -local -allowGacDeployment -url %TargetWebUrl%

    goto :EOF


@rem----------------------------------------------------------------------
@rem    LRetract
@rem----------------------------------------------------------------------
:LRetract
    echo Retracting solution %PackageName% ...
    "%SPAdminTool%" -o retractsolution -name "%PackageName%" -local -url %TargetWebUrl%

    echo Deleting solution %PackageName% from SharePoint ...
    "%SPAdminTool%" -o deletesolution -name "%PackageName%"

    goto :EOF


@rem----------------------------------------------------------------------
@rem    LTerminate
@rem----------------------------------------------------------------------
:LTerminate
    set UserInput=
    set /P UserInput=Hit enter key to quit.

    set SPLocation=
    set SPAdminTool=
    set PackageFile=
    set PackageName=
    set Install=
    set Uninstall=
    set TargetSiteUrl=
    set TargetWebUrl=
    set SPTemplateLocation=
    set SPFeaturesLocation=
    set SPSiteTemplateLocation=
    set SPWebTempFileLocation=
    set ValidationFailed=
    set UserInput=


popd
endlocal

