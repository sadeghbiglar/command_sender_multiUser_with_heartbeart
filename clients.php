<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Clients Table</title>
    <style>
        table {
            border-collapse: collapse;
            width: 100%;
            margin: 20px 0;
        }
        th, td {
            border: 1px solid #ddd;
            padding: 8px;
            text-align: center;
        }
        th {
            background-color: #f4f4f4;
        }
        .green-row {
            background-color: #d4edda; /* سبز روشن */
        }
        .red-row {
            background-color: #f8d7da; /* قرمز روشن */
        }
    </style>
    <script>
        // هر 10 ثانیه صفحه را رفرش می‌کنیم
        setInterval(() => {
            location.reload();
        }, 10000); // 10,000 میلی‌ثانیه = 10 ثانیه
    </script>
</head>
<body>
    <h1>Clients Table</h1>
    <table>
        <thead>
            <tr>
                <th>ID</th>
                <th>IP Address</th>
                <th>Client Name</th>
                <th>Last Seen</th>
            </tr>
        </thead>
        <tbody>
        <?php
        // تنظیم منطقه زمانی PHP
        date_default_timezone_set("Asia/Tehran"); // تنظیم منطقه زمانی برای ایران

        // اطلاعات دیتابیس
        $host = "localhost";     // آدرس دیتابیس
        $user = "root";          // نام کاربری
        $password = "";          // رمز عبور
        $database = "clientsdb"; // نام دیتابیس

        // اتصال به دیتابیس
        $conn = new mysqli($host, $user, $password, $database);

        // بررسی اتصال
        if ($conn->connect_error) {
            die("Connection failed: " . $conn->connect_error);
        }

        // خواندن اطلاعات از جدول
        $sql = "SELECT id, ip_address, client_name, last_seen FROM clients";
        $result = $conn->query($sql);

        if ($result->num_rows > 0) {
            // نمایش اطلاعات
            while ($row = $result->fetch_assoc()) {
                $id = $row['id'];
                $ipAddress = $row['ip_address'];
                $clientName = $row['client_name'];
                $lastSeen = $row['last_seen'];

                // تبدیل last_seen به تایم‌استمپ
                $lastSeenTimestamp = strtotime($lastSeen); // تبدیل رشته به تایم‌استمپ
                $currentTimestamp = time(); // زمان فعلی سرور PHP
                $timeDiff = $currentTimestamp - $lastSeenTimestamp; // محاسبه اختلاف زمانی

                // بررسی وضعیت ردیف
                $rowClass = ($timeDiff > 10) ? "red-row" : "green-row"; // اگر بیش از 10 ثانیه گذشته باشد قرمز، در غیر این صورت سبز

                // نمایش ردیف در جدول
                echo "<tr class='$rowClass'>";
                echo "<td>$id</td>";
                echo "<td>$ipAddress</td>";
                echo "<td>$clientName</td>";
                echo "<td>$lastSeen</td>";
                echo "</tr>";
            }
        } else {
            // اگر دیتایی موجود نباشد
            echo "<tr><td colspan='4'>No data available</td></tr>";
        }

        // بستن اتصال دیتابیس
        $conn->close();
        ?>
        </tbody>
    </table>
</body>
</html>
