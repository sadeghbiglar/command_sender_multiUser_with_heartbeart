<!DOCTYPE html>
<html>
<head>
    <title>Send Command</title>
</head>
<body>
    <h1>Send Command to Controller</h1>
    <form method="post">
        <label>Server IP:</label>
        <input type="text" name="ip" required><br><br>
        <label>Command:</label>
        <input type="text" name="command" required><br><br>
        <button type="submit">Send Command</button>
    </form>

    <?php
    if ($_SERVER["REQUEST_METHOD"] == "POST") {
        $ip = $_POST["ip"];
        $command = $_POST["command"];

        $url = "http://localhost:8000/"; // آدرس API

        $data = http_build_query([
            "ip" => $ip,
            "command" => $command
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
