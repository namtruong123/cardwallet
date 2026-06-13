SELECT Email, COUNT(*)
FROM users
GROUP BY Email
HAVING COUNT(*) > 1;

SELECT PhoneNumber, COUNT(*)
FROM users
GROUP BY PhoneNumber
HAVING COUNT(*) > 1;

SELECT Id, FullName, Email, PhoneNumber, Status
FROM users
WHERE Email = 'vinhvinh0257@gmail.com'
   OR PhoneNumber = '090424141';
