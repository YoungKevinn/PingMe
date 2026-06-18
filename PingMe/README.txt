trước khi dùng , tạo 1 user ping me trước nha mấy th l
CREATE DATABASE IF NOT EXISTS pingme_dev CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS 'pingme'@'%' IDENTIFIED WITH mysql_native_password BY 'pingme123';
GRANT ALL PRIVILEGES ON pingme_dev.* TO 'pingme'@'%';
FLUSH PRIVILEGES;v  