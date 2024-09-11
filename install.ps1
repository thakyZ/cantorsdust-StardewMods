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
  [ValidateSet('Release','Debug')]
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
          If (-not [Directory]::Exists("$(Join-Path -Path $Item.Directory -ChildPath 'i18n')")) {
            Write-Warning -Message "Found valid install location at $($Item.Directory.FullName) but could not find translations folder.`nPlease make sure this is the correct directory.";
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
      New-Variable -Scope Local -Name 'PathBackup' -Option ReadOnly -Value "$($env:Path)" -Description "The Backup of the environment variable %PATH%." -ErrorAction Continue;

      If ($Null -eq (Get-Variable -Scope Local -Name 'PathBackup' -ErrorAction SilentlyContinue)) {
        Throw [Exception]::new('Failed to create the variable $script:PathBackup');
      }
      $DotNet = (Get-Command -Name 'dotnet' -ErrorAction Continue);
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
      $env:Path = (Get-Variable -Scope Local -Name 'PathBackup' -ErrorAction Continue -ValueOnly);
      Remove-Variable -Scope Local -Name 'PathBackup' -Force -ErrorAction Continue;
    }
  }

  If ($Null -eq (Get-Command -Name Trim-StringStart -ErrorAction SilentlyContinue)) {
    Function Trim-StringStart {
      [CmdletBinding()]
      Param(
        [Parameter(Mandatory = $True,
                   Position = 0,
                   ValueFromPipeline = $True,
                   HelpMessage = 'The string to trim another string from the start of.')]
        [ValidateNotNullOrEmpty()]
        [string]
        $String,
        [Parameter(Mandatory = $True,
                   Position = 1,
                   HelpMessage = 'The string to trim from the start of the String argument.')]
        [ValidateNotNullOrEmpty()]
        [string]
        $ToRemove
      )

      Begin {
        [string] $Output = $String;
      } Process {
        ForEach ($Char in $ToRemove) {
          If ($Char -isnot [char]) {
            Throw [Exception]::new('For whatever reason an item in the string property -ToRemove is not type of [System.Char]');
          }

          $Output.TrimStart($Char);
        }
      } End {
        Return $Output;
      }
    }
  }

  [string] $Mod               = $PSBoundParameters['Mod'];
  [string] $DotNet            = (Find-DotNet);
  New-Variable -Scope Script -Name 'PathBackup' -Option ReadOnly -Value "$($env:Path)" -Description "The Backup of the environment variable %PATH%." -ErrorAction Continue;

  If ($Null -eq (Get-Variable -Scope Script -Name 'PathBackup' -ErrorAction SilentlyContinue)) {
    Throw [Exception]::new('Failed to create the variable $script:PathBackup');
  }

  # $7Zip                     = (Get-Command -Name '7z' -ErrorAction SilentlyContinue);
  [string] $BuildOutput       = (Join-Path -Path $PSScriptRoot -ChildPath $Mod -AdditionalChildPath @('bin',"$($Configuration)",'net6.0',"$($Mod).dll"));
  [string] $ManifestFile      = (Join-Path -Path $PSScriptRoot -ChildPath $Mod -AdditionalChildPath @('manifest.json'));
  [string] $TranslationDir    = (Join-Path -Path $PSScriptRoot -ChildPath $Mod -AdditionalChildPath @('i18n'));

  If ([string]::IsNullOrEmpty($Destination)) {
    $Destination = (Join-Path -Path $env:MO2_PROFILES -ChildPath 'StardewValley' -AdditionalChildPath @('mods', $Mod, $Mod));
  }

  If (-not (Test-Path -LiteralPath $Destination -PathType Container)) {
    Throw [Exception]::new('Please specify a destination directory.');
  }

  If ($Null -eq (Get-ChildItem -LiteralPath $Destination | Where-Object { $_.Name -eq "$($Mod).dll" -or $_.Name -eq 'i18n' })) {
    [string] $FoundDll = (Find-Dll -Destination $Destination);

    If ([string]::IsNullOrEmpty($FoundDll)) {
      Throw [Exception]::new("Could not find valid dll file in $($Destination).");
    }

    $Destination = $FoundDll;
  }

  If ([string]::IsNullOrEmpty($DotNet)) {
    Throw [Exception]::new("Failed to find 'dotnet.exe' on the system path.");
  }

<#
  If ($Null -eq $7Zip) {
    Write-Warning -Message 'Failed to find 7-Zip on system path. Will not be packaging the mod.';
  }
#>
  [string] $ModProjDir    = (Join-Path -Path $PSScriptRoot -ChildPath $Mod);
  [string] $ModCsProj     = (Join-Path -Path $ModProjDir   -ChildPath "$($Mod).csproj");
  [string] $ReleasesDir   = (Join-Path -Path $PSScriptRoot -ChildPath '_releases');
  [string] $OutputFile    = (Join-Path -Path $ReleasesDir  -ChildPath "$($Mod) 0.0.0.zip");

  If (-not [Directory]::exists($ModProjDir)) {
    Throw [Exception]::new("Unexpected error occurred, could not find the mod project directory. Expected directory at `"$($ModProjDir)`".");
  }

  If (-not [File]::Exists($ModIsProj)) {
    $Items = (Get-ChildItem -LiteralPath $ModProjDir -File -Filter '*.*proj' | Where-Object { $_.BaseName -eq $Mod });
    If ($Items.Count -eq 0) {
      Throw [Exception]::new("Unexpected error occurred, could not find the mod project file. Expected file at `"$($ModIsProj)`" or an equilvalent.");
    } ElseIf ($Items.Count -gt 1) {
      Throw [Exception]::new("Unexpected error occurred, too many mod project files. Found $($Items.Length) at:`n$([string]::Join("`n", $Items))");
    }

    $ModCsProj = $Items[0].FullName;
  }
} Process {
  [Process] $DotNetProcess = (Start-Process -NoNewWindow -FilePath $DotNet -Wait -ArgumentList @('build', $ModCsProj, "-property:Configuration=$($Configuration)", "-property:SolutionDir=$($PWD)") -PassThru);

  If ($DotNetProcess.ExitCode -ne 0) {
    Throw [Exception]::new("DotNet exited with exit code $($DotNetProcess.ExitCode).");
  }

<#If ($Null -ne $7Zip) {
    [string] $OutputDir = (Join-Path -Path $PSScriptRoot -ChildPath 'dist');
    If (-not [Directory]::Exists($OutputDir)) {
      $OutputDir = (New-Item -Path $OutputDir -ItemType Directory).FullName;
    }
    [string] $OutputFile = (Join-Path -Path $OutputDir -ChildPath "$($Mod).zip");
    If ([File]::Exists($OutputFile)) {
      Remove-Item -LiteralPath $OutputFile -Force -ErrorAction Stop;
    }
    [Process] $7ZipProcess = (Start-Process -NoNewWindow -FilePath $7Zip.Source -Wait -ArgumentList @('a', $OutputFile, $TranslationDir, $BuildOutput, $ManifestFile) -PassThru);
    If ($7ZipProcess.ExitCode -ne 0) {
      Write-Error -Message "7-Zip exited with exit code $($7ZipProcess.ExitCode).";
      Exit 1;
    }
    Write-Host -Object "Mod packaged to file at: `"$($OutputFile)`"";
  }#>

  If  (-not [File]::Exists($OutputFile)) {
    $Items = (Get-ChildItem -LiteralPath $ReleasesDir -File -Filter '*.zip' | Where-Object { $_.BaseName.StartsWith($Mod) } | Select-Object -Property FullName,Name,Extension,@{Name='Version';Expression={
      [string]  $VersionString = (Trim-StringStart -String $_.BaseName -ToTrim "$($Mod) ");
      [Versoon] $Version = $Null;
      If (-not [Version]::TryParse($VersionString, [ref] $Version)) {
        Write-Error -Message "Failed to parse version string of value `"$($VersionString)`" to type of [System.Version]." | Out-Host;
        Return $Null;
      }
      Return $Version
    }});
    If ($Items.Count -eq 0) {
      Write-Host -Object "`$Items = (";
      Write-Output -InputObject $Items | Out-Host;
      Write-Host -Object ");";
      Throw [Exception]::new("Unexpected error occurred, could not find the mod output archive. Expected file at `"$($OutputFile)`" or an equilvalent.");
    } ElseIf ($Items.Count -gt 1) {
      $ItemsSorted = ($Items | Sort-Object -Property Version);
      $Items = $ItemsSorted;
    }

    $OutputFile = $Items[0].FullName;
  }

  Write-Host -Object "Mod packaged to file at: `"$($OutputFile)`"";

  Write-Host -Object "Copied to $($Destination)";

  Copy-Item -LiteralPath $TranslationDir -Recurse -Force -Destination $Destination -ErrorAction Stop;
  Copy-item -LiteralPath $BuildOutput -Force -Destination (Join-Path -Path $Destination -ChildPath "$($Mod).dll") -ErrorAction Stop;
  Copy-item -LiteralPath $ManifestFile -Force -Destination (Join-Path -Path $Destination -ChildPath 'manifest.json') -ErrorAction Stop;
} End {
} Clean {
  $env:Path = (Get-Variable -Scope Script -Name 'PathBackup' -ErrorAction Continue -ValueOnly);
  Remove-Variable -Scope Script -Name 'PathBackup' -Force -ErrorAction Continue;
}
