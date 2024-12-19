<!DOCTYPE html>
<html>
<head>
    <title>Upload and Send Command</title>
</head>
<body>
    <h1>Step 1: Upload Your File</h1>
    <?php
    $fileUploaded = false; // متغیر برای بررسی وضعیت آپلود فایل
    $uploadedFilePath = "";

    if ($_SERVER["REQUEST_METHOD"] == "POST" && isset($_FILES["uploaded_file"])) {
        $path = "upload/";
        $uploadedFilePath = $path . basename($_FILES['uploaded_file']['name']);

        if (move_uploaded_file($_FILES['uploaded_file']['tmp_name'], $uploadedFilePath)) {
            echo "<p style='color: green;'>The file " . basename($_FILES['uploaded_file']['name']) . " has been uploaded successfully.</p>";
            $fileUploaded = true; // فایل با موفقیت آپلود شده است
        } else {
            echo "<p style='color: red;'>There was an error uploading the file, please try again!</p>";
        }
    }
    ?>

    <!-- فرم آپلود فایل -->
    <?php if (!$fileUploaded): ?>
        <form enctype="multipart/form-data" method="POST">
            <p>Upload your file</p>
            <input type="file" name="uploaded_file" required><br />
            <input type="submit" value="Upload">
        </form>
    <?php endif; ?>

    <!-- فرم ارسال اطلاعات در صورت موفقیت آپلود فایل -->
    <?php if ($fileUploaded): ?>
        <h1>Step 2: Send Command to Controller</h1>
        <form method="POST">
            <input type="hidden" name="filePath" value="<?php echo htmlspecialchars($uploadedFilePath); ?>">
            <label>Server IP:</label>
            <input type="text" name="ip" required><br><br>
            <label>Destination Path:</label>
            <input type="text" name="dest" required><br><br>
            <button type="submit" name="sendCommand">Send</button>
        </form>
    <?php endif; ?>

    <!-- ارسال اطلاعات به API -->
    <?php
    if ($_SERVER["REQUEST_METHOD"] == "POST" && isset($_POST["sendCommand"])) {
        $ip = $_POST["ip"];
        $dest = $_POST["dest"];
        $filePath = $_POST["filePath"];

        $url = "http://localhost:8001/"; // آدرس API

        $data = http_build_query([
            "ip" => $ip,
            "path" => $filePath,
            "dest" => $dest
        ]);

        $options = [
            "http" => [
                "header" => "Content-type: application/x-www-form-urlencoded",
                "method" => "POST",
                "content" => $data
            ]
        ];

        $context = stream_context_create($options);
        $response = file_get_contents($url, false, $context);

        if ($response === FALSE) {
            echo "<p style='color: red;'>Error sending command.</p>";
        } else {
            echo "<h3>Result:</h3>";
            echo "<pre>$response</pre>";
        }
    }
    ?>
</body>
</html>
