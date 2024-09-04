using namespace System;
using namespace System.Collections.ObjectModel;
using namespace System.Diagnostics;
using namespace System.IO;
using namespace System.Management.Automation;

[CmdletBinding()]
Param(
  [Parameter(Mandatory = $False,
             Position = 1,
             HelpMessage = 'Destination directory for installing the mod.')]
  [AllowNull()]
  [string]
  $Destination = $Null,
  [Parameter(Mandatory = $False,
             Position = 2,
             HelpMessage = 'Build type for building the mod.')]
  [ValidateSet('Releae','Debug')]
  [string]
  $Configuration = 'Release'
)

DynamicParam {
  [ParameterAttribute] $ModAttribute = [ParameterAttribute]::new();
  $ModAttribute.Position = 0;
  $ModAttribute.Mandatory = $True;
  $ModAttribute.HelpMessage = 'The name of the mod to install and package.';

  [ValidateNotNullOrEmptyAttribute] $ValidateAttribute = [ValidateNotNullOrEmptyAttribute]::new();

  [string[]] $Files = (Get-ChildItem -LiteralPath $PSScriptRoot -Directory | Where-Object { $Dir = $_; Return ($Null -ne (Get-ChildItem -LiteralPath $_ -File -Filter '*.csproj' | Where-Object { $_.BaseName -eq $Dir.Name})); }).Name
  [ValidateSetAttribute] $ValidateAttribute = [ValidateSetAttribute]::new($Files);

  [Collection[Attribute]] $AttributeCollection = [Collection[Attribute]]::new();
  $AttributeCollection.Add($ModAttribute);
  $AttributeCollection.Add($ValidateAttribute);
  $AttributeCollection.Add($SetAttribute);

  [RuntimeDefinedParameter] $ModParam = [RuntimeDefinedParameter]::new('Mod', [string], $AttributeCollection);

  [RuntimeDefinedParameterDictionary] $ParamDictionary = [RuntimeDefinedParameterDictionary]::new();
  $ParamDictionary.Add('Mod', $ModParam)

  return $ParamDictionary
} Begin {
  $Mod = $PSBoundParameters['Mod'];
  
  If ([string]::IsNullOrEmpty($Destination)) {
    $Destination = (Join-Path -Path $env:MO2_PROFILES -ChildPath 'StardewValley' -AdditionalChildPath @('mods', 'TimeSpeed', 'TimeSpeed'));
  }

  If (-not (Test-Path -LiteralPath $Destination -PathType Container)) {
    Write-Error -Message "Please specify a destination directory.";
    Exit 1;
  }

  If ($Null -eq (Get-ChildItem -LiteralPath $Destination | Where-Object { $_.Name -eq "$($Mod).dll" -or $_.Name -eq 'i18n' })) {
    Function Find-Dll {
      [CmdletBinding()]
      [OutputType([string])]
      Param(
        [Parameter(Mandatory = $True)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Destination
      )

      Begin {
        [string] $Output = $Null;
      } Process {
        $Items = (Get-ChildItem -LiteralPath $Destination -File -Filter '*.dll' -Recurse);
        ForEach ($Item in $Items) {
          If ($Item.Name -eq "$($Mod).dll") {
            If (-not [Directory]::Exists("$(Join-Path -Path $Item.Directory -ChildItem 'i18n')")) {
              Write-Warning -Message "Found valid install location at $(ITem.Directory.FullName) but could not find translations folder.`nPlease make sure this is the correct directory.";
              Write-Host -Object 'Press any key to continue...';
              Read-Host;
            }
            $Output = $Item.Directory.FullName;
          }
        }
      } End {
        Return $Output;
      }
    }

    [string] $FoundDll = (Find-Dll -Destination $Destination);

    If ([string]::IsNullOrEmpty($FoundDll)) {
      Write-Error -Message "Could not find valid dll file in $($Destination).";
      Exit 1;
    }

    $Destination = $FoundDll;
  }

  $script:PathBackup = $env:Path;

  Function Find-DotNet {
    [CmdletBinding()]
    [OutputType([string])]
    Param(
      [Parameter(Mandatory = $False)]
      [AllowNull()]
      [string]
      $Path = $Null
    )

    Begin {
      [string] $Output = $Null;
      $local:PathBackup = $env:Path;
      $DotNet = (Get-Command -Name 'dotnet' -ErrorAction SilentlyContinue);
      $UpdatePathVariable = (Get-Command -Name 'Update-PathVariable' -ErrorAction SilentlyContinue);
    } Process {
      If ($Null -ne $DotNet) {
        If ((Get-Item -LiteralPath $DotNet.Source).FullName -match '[\\/]mono[\\/]') {
          If ($Null -ne $UpdatePathVariable) {
            Update-PathVariable;
            $Output = (Find-DotNet -Path $env:Path);
          } Else {
            Write-Warning -Message "Found 'dotnet.exe' in a mono compiler directory only, make sure this is right.";
            Write-Host -Object 'Press any key to continue...';
            Read-Host;
          }
        } Else {
          $Output = $DotNet.Source;
        }
      }
    } End {
      Return $Output;
    } Clean {
      $env:Path = $script:PathBackup;
      Remove-Variable -Scope Local -Name 'PathBackup' -ErrorAction SilentlyContinue;
    }
  }

  [string] $DotNet = (Find-DotNet);
  $env:Path = $script:PathBackup;
  If ([string]::IsNullOrEmpty($DotNet)) {
    Write-Error -Message "Failed to find 'dotnet.exe' on the system path.";
    Exit 1;
  }

  $7Zip = (Get-Command -Name '7z' -ErrorAction SilentlyContinue);
  [string] $BuildOutput = (Join-Path -Path $PSScriptRoot -ChildPath $Mod -AdditionalChildPath @('bin',"$($Configuration)",'net6.0',"$($Mod).dll"));
  [string] $ManifestFile = (Join-Path -Path $PSScriptRoot -ChildPath $Mod -AdditionalChildPath @('manifest.json'));
  [string] $TranslationDir = (Join-Path -Path $PSScriptRoot -ChildPath $Mod -AdditionalChildPath @('i18n'));

  If ($Null -eq $7Zip) {
    Write-Warning -Message 'Failed to find 7-Zip on system path. Will not be packaging the mod.';
  }
} Process {
  [Process] $DotNetProcess = (Start-Process -NoNewWindow -FilePath $DotNet -Wait -ArgumentList @('build', "$(Join-Path -Path $PSScriptRoot -ChildPath 'Cantorsdust.StardewMods.sln')", "-property:Configuration=$($Configuration)") -PassThru);
  While (-not $DotNetProcess.HasExited) {
    Start-Sleep -Seconds 5;
  }
  If ($DotNetProcess.ExitCode -ne 0) {
    Write-Error -Message "DotNet exited with exit code $($DotNetProcess.ExitCode).";
    Exit 1;
  }
  If ($Null -ne $7Zip) {
    [string] $OutputDir = (Join-Path -Path $PSScriptRoot -ChildPath 'dist');
    If (-not [Directory]::Exists($OutputDir)) {
      $OutputDir = (New-Item -Path $OutputDir -ItemType Directory).FullName;
    }
    [string] $OutputFile = (Join-Path -Path $OutputDir -ChildPath "$($Mod).zip");
    If ([File]::Exists($OutputFile)) {
      Remove-Item -LiteralPath $OutputFile -Force -ErrorAction Stop;
    }
    [Process] $7ZipProcess = (Start-Process -NoNewWindow -FilePath $7Zip.Source -Wait -ArgumentList @('a', $OutputFile, $BuildOutput, $TranslationDir, $ManifestFile) -PassThru);
    While (-not $7ZipProcess.HasExited) {
      Start-Sleep -Seconds 5;
    }
    If ($7ZipProcess.ExitCode -ne 0) {
      Write-Error -Message "7-Zip exited with exit code $($7ZipProcess.ExitCode).";
      Exit 1;
    }
    Write-Host -Object "Mod packaged to file at: `"$($OutputFile)`"";
  }

  Copy-Item -LiteralPath $TranslationDir -Recurse -Force -Destination $Destination -ErrorAction Stop;
  Copy-item -LiteralPath $BuildOutput -Force -Destination (Join-Path -Path $Destination -ChildPath "$($Mod).dll") -ErrorAction Stop;
  Copy-item -LiteralPath $ManifestFile -Force -Destination (Join-Path -Path $Destination -ChildPath 'manifest.json') -ErrorAction Stop;
} End {
} Clean {
  $env:Path = $script:PathBackup;
  Remove-Variable -Scope Script -Name 'PathBackup' -ErrorAction SilentlyContinue;
}