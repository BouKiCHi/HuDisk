
Write-Output "--- HuDisk test ---"
$testSrcFile = "TEST_SRC.TXT"
$testFile = "TEST.TXT"
$dataDirectory = "data"

function hudisk($opt,$image,$file,$opts) {
    Write-Output ("Option:" + $opt + " Image:" + $image + " File:" + $file)
    ../../hudisk $opt $image $file $opts
}

function x1save($image,$file) {
    Write-Output ("x1save:" + $image + " file:" + $file);
    ./msdos.exe ./x1save.exe $image $file
}

function getDataPath($name) {
    return Join-Path $dataDirectory $name
}


function testFormat($image) {
    Write-Output ("testFormat:" + $image)    
    hudisk --format $image ""
}


function testImage($image) {
    Write-Output ("testImage:" + $image)
    if (Test-Path $image) {
        Remove-Item -Force $image
    }
    $srcPath = getDataPath $testSrcFile
    Copy-Item -Force $srcPath $testFile

    hudisk "-a" $image $testFile
    Remove-Item $testFile
    hudisk "-x" $image $testFile

    $srcData = (Get-Content $srcPath)
    $compare = Compare-Object $srcData (Get-Content $testFile)

    $result = "FAIL"
    if ($compare -eq $NULL) {
        $result = "OK"
    }
    Write-Output ("TEST RESULT:" + $result)
}


function testOver64kImage($image) {
    Write-Output ("testOver64kImage:" + $image)
    if (Test-Path $image) {
        Remove-Item -Force $image
    }
    $srcPath = getDataPath "256.BIN"
    $srcTextPath = getDataPath "TEST_SRC.TXT"
    $test64kBin = "TEST64K.BIN"

    $tmp = (Get-Content -Encoding Byte $srcPath)
    $tmp2 = $tmp
    0..254 | ForEach-Object { $tmp += $tmp2 }
    $tmp += (Get-Content -Encoding Byte $srcTextPath)
    $tmp | Set-Content $test64kBin -Encoding Byte 

    Write-Output  "Append..."
    hudisk "-a" $image $test64kBin
    Remove-Item $test64kBin
     Write-Output  "Extract..."
    hudisk "-x" $image $test64kBin

    Write-Output  "Compare..."
    $resultData = (Get-Content -Encoding Byte $test64kBin)
    $compare = Compare-Object $tmp $resultData
    $result = "FAIL"
    if ($compare -eq $NULL) {
        $result = "OK"
    }
    Write-Output ("TEST RESULT:" + $result)
}

function testBoundary($image,$x1s) {
    Write-Output ("boundImage:" + $image)
    if (Test-Path $image) {
        Remove-Item $image
    }

    $filenames =  "255.bin" , "256.bin" , "257.bin" , "511.bin" , "512.bin" , "513.bin" , "767.bin" , "768.bin" , "769.bin"

    foreach($name in $filenames) {
        $path = getDataPath $name
        hudisk "-a" $image $path $x1s
    }
    hudisk "-l" $image 
}


# X1SAVEでのデータ
function testBoundaryX1S($image) {
    Write-Output ("boundImage:" + $image)
    if (Test-Path $image) {
        Remove-Item $image
    }
    hudisk "--format" $image

    $filenames =  "255.bin" , "256.bin" , "257.bin" , "511.bin" , "512.bin" , "513.bin" , "767.bin" , "768.bin" , "769.bin"

    foreach($name in $filenames) {
        $path = getDataPath $name
        x1save $image $path
    }

    hudisk "-l" $image
}

function fillImage($image) {
    Write-Output ("fillImage:" + $image)
    if (Test-Path $image) {
        Remove-Item $image
    }

    $path = getDataPath "HELLO.txt"
    0..81 | ForEach-Object {
        $name = "{0:000}.BIN" -F $_
        $opts = "--name",$name
        hudisk "-a" $image $path $opts
    }
    hudisk "-l" $image 
}

function inOutTest() {
    $imagePlainName = "TEST.2D"
    $imageName = "TEST.D88"

    testImage $imageName
    testImage $imagePlainName
}

function boundTest() {
    # testBoundary "BOUND.D88" ""
    # testBoundaryX1S "BOUND_X1S.D88"
    testBoundary "BOUND_X1SC.D88" "--x1s"
}

Write-Output "Version Check"
hudisk -h
# testFormat "FORMAT.D88"
# testImage "TEST.D88"
# fillImage "TESTFILL.D88"
# boundTest
testOver64kImage "TEST.D88"




