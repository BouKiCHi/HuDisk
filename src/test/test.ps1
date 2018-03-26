
Write-Output "HuDisk test"
$testSrcFile = "TEST_SRC.TXT"
$testFile = "TEST.TXT"

function testImage($image) {
    Write-Output ("testImage:" + $image)
    Copy-Item -Force $testSrcFile $testFile
    if (Test-Path $image) {
        Remove-Item $image
    }
    ../../hudisk -a $image $testFile
    Remove-Item .\$testFile
    ../../hudisk -x $image $testFile
    $compare = Compare-Object (Get-Content $testSrcFile) (Get-Content $testFile)
    
    if ($compare -eq $NULL) {
        Write-Output "OK"
    }        
}

function testBoundary($image,$x1s) {
    Write-Output ("boundImage:" + $image)
    if (Test-Path $image) {
        Remove-Item $image
    }
    ../../hudisk -a $x1s $image "255.bin"
    ../../hudisk -a $x1s $image "256.bin"
    ../../hudisk -a $x1s $image "257.bin"
    ../../hudisk -a $x1s $image "511.bin"
    ../../hudisk -a $x1s $image "512.bin"
    ../../hudisk -a $x1s $image "513.bin"
    ../../hudisk -a $x1s $image "767.bin"
    ../../hudisk -a $x1s $image "768.bin"
    ../../hudisk -a $x1s $image "769.bin"
    ../../hudisk -l $image 
}


function x1save($image,$file) {
    Write-Output ("x1save:" + $image + " file:" + $file);
    ./msdos.exe ./x1save.exe $image $file

}

function testBoundaryX1S($image) {
    Write-Output ("boundImage:" + $image)
    if (Test-Path $image) {
        Remove-Item $image
    }
    ../../hudisk --format $image
    x1save $image "255.bin"
    x1save $image "256.bin"
    x1save $image "257.bin"
    x1save $image "511.bin"
    x1save $image "512.bin"
    x1save $image "513.bin"
    x1save $image "767.bin"
    x1save $image "768.bin"
    x1save $image "769.bin"
    ../../hudisk -l $image
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

boundTest




