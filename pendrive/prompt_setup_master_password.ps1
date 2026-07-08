$secure1 = Read-Host "Choose a master password for encrypted secrets" -AsSecureString
$secure2 = Read-Host "Confirm master password" -AsSecureString

function Convert-SecureStringToPlain([Security.SecureString]$value) {
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($value)
    try { [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally {
        if ($bstr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
    }
}

$plain1 = Convert-SecureStringToPlain $secure1
$plain2 = Convert-SecureStringToPlain $secure2

if ([string]::IsNullOrWhiteSpace($plain1)) {
    Write-Error "Master password cannot be empty."
    exit 1
}

if ($plain1 -ne $plain2) {
    Write-Error "Master passwords do not match."
    exit 1
}

Write-Output $plain1
