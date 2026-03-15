# 📋 Hướng Dẫn Format PDF Để Import Câu Hỏi Trắc Nghiệm

## ✅ Định Dạng Hỗ Trợ

Chức năng import PDF chỉ hỗ trợ **câu hỏi trắc nghiệm (MCQ)** với **4 đáp án (A, B, C, D)**.

---

## 📄 Format Tiêu Chuẩn

### Ví Dụ 1: Format Cơ Bản

```
1. Thủ đô của Pháp là gì?
A) London
B) Paris
C) Berlin
D) Madrid
Answer: B

2. 2 + 2 bằng bao nhiêu?
A) 3
B) 4
C) 5
D) 6
Answer: B

3. Nước Anh nằm ở châu nào?
A) Châu Á
B) Châu Âu
C) Châu Mỹ
D) Châu Phi
Answer: B
```

---

### Ví Dụ 2: Format Với Dấu Chấm

```
1. What is the capital of France?
A. London
B. Paris
C. Berlin
D. Madrid
Answer: B

2. What is 2 + 2?
A. 3
B. 4
C. 5
D. 6
Answer: B
```

---

### Ví Dụ 3: Format Với Dấu Ngoặc

```
1) What is the capital of France?
A) London
B) Paris
C) Berlin
D) Madrid
Answer: B

2) What is 2 + 2?
A) 3
B) 4
C) 5
D) 6
Answer: B
```

---

### Ví Dụ 4: Format Có Đáp Án Tiếng Việt

```
1. Thủ đô của Pháp là gì?
A) Luân Đôn
B) Paris
C) Berlin
D) Madrid
Đáp án: B

2. 2 + 2 bằng bao nhiêu?
A) 3
B) 4
C) 5
D) 6
Đáp án: B
```

---

## 🎯 Quy Tắc Bắt Buộc

### ✅ Bắt Buộc Có

1. **Mỗi câu hỏi phải có:**
   - Số thứ tự (1, 2, 3, ...) hoặc dấu chấm/ngoặc (1. hoặc 1))
   - Nội dung câu hỏi
   - **4 đáp án (A, B, C, D)** - Không được thiếu
   - **Đáp án đúng** (Answer: B hoặc Đáp án: B)

2. **Đáp án phải là:**
   - A, B, C, hoặc D
   - Không: 1, 2, 3, 4
   - Không: True, False

### ⚠️ Không Hỗ Trợ

- ❌ Câu hỏi TRUE/FALSE
- ❌ Câu hỏi tự luận (ESSAY)
- ❌ Câu hỏi điền vào chỗ trống
- ❌ Câu hỏi có ít hơn 4 đáp án
- ❌ Câu hỏi không có đáp án

---

## 🔥 Ví Dụ Đầy Đủ (Có Thể Copy-Paste)

```
1. Thủ đô của Anh là gì?
A) London
B) Paris
C) Berlin
D) Madrid
Answer: A

2. Những gì sau đây là chất dinh dưỡng?
A) Protein
B) Vitamin
C) Canxi
D) Tất cả các câu trên
Answer: D

3. Người sáng lập ra Facebook là ai?
A) Bill Gates
B) Steve Jobs
C) Mark Zuckerberg
D) Elon Musk
Answer: C

4. Đơn vị của sức mạnh là gì?
A) Joule
B) Watt
C) Newton
D) Volt
Answer: C

5. Nước Nhật là một đảo quốc?
A) Đúng
B) Sai
C) Không chắc
D) Không biết
Answer: A
```

---

## 📐 Các Format Được Chấp Nhận

Hệ thống hỗ trợ các biến thể của format:

```
# Format 1: Dấu ngoặc tròn + chữ A)
1. Question?
A) Answer A
B) Answer B
C) Answer C
D) Answer D
Answer: A

# Format 2: Dấu chấm + chữ A.
1. Question?
A. Answer A
B. Answer B
C. Answer C
D. Answer D
Answer: A

# Format 3: Dấu ngoặc đơn + chữ 1)
1) Question?
A) Answer A
B) Answer B
C) Answer C
D) Answer D
Answer: A

# Format 4: Đáp án trong ngoặc
1. Question?
A) Answer A
B) Answer B  
C) Answer C
D) Answer D
Correct: A
```

---

## ✨ Mẹo Để Đạt Kết Quả Tốt Nhất

1. **Giữ khoảng cách:** Có ít nhất một dòng trống giữa các câu hỏi
2. **Dùng format nhất quán:** Tất cả câu hỏi dùng cùng một format
3. **Viết sạch:** Không có ký tự lạ hoặc ký tự đặc biệt không cần thiết
4. **Kiểm tra lại:** Chắc chắn tất cả 4 đáp án được điền
5. **Kiểm tra đáp án:** Đáp án phải là A, B, C hoặc D

---

## 📊 Bảng So Sánh: Hỗ Trợ vs Không Hỗ Trợ

| Loại Câu Hỏi | Hỗ Trợ | Ghi Chú |
|--------------|--------|--------|
| MCQ (4 đáp án) | ✅ | Được hỗ trợ 100% |
| MCQ (3 đáp án) | ❌ | Cần đầy đủ 4 đáp án |
| TRUE/FALSE | ❌ | Cần MCQ với 4 đáp án |
| Tự luận (ESSAY) | ❌ | Chỉ hỗ trợ MCQ |
| Điền vào chỗ trống | ❌ | Chỉ hỗ trợ MCQ |
| Matching | ❌ | Chỉ hỗ trợ MCQ |

---

## 🛠️ Cách Sử Dụng

### Step 1: Chuẩn Bị PDF
- Dùng Word, Google Docs hoặc text editor viết câu hỏi theo format trên
- Xuất thành PDF

### Step 2: Upload PDF
- Mở trang **Ngân hàng Câu hỏi**
- Click nút **"Import"** → Chọn **"PDF (.pdf)"**
- Chọn file PDF vừa chuẩn bị

### Step 3: Kiểm Tra Kết Quả
- Hệ thống sẽ parse PDF và hiển thị kết quả
- Xem số câu hỏi được import thành công
- Nếu có lỗi, xem chi tiết lỗi và sửa PDF

### Step 4: Phê Duyệt Câu Hỏi
- Đi vào từng câu hỏi vừa import
- Thêm subject, tag, và thông tin bổ sung
- Publish câu hỏi

---

## ⚠️ Lỗi Phổ Biến

### ❌ Lỗi: "No valid MCQ questions found"

**Nguyên nhân:** 
- Câu hỏi không có đầy đủ 4 đáp án
- Không có dòng "Answer: X"
- Format không đúng

**Giải pháp:**
- Kiểm tra mỗi câu hỏi có đầy đủ A, B, C, D
- Thêm dòng "Answer: X" cho mỗi câu
- Dùng format tiêu chuẩn

### ❌ Lỗi: "File size exceeds 50MB limit"

**Nguyên nhân:** PDF file quá lớn

**Giải pháp:**
- Chia file thành nhiều file nhỏ hơn
- Loại bỏ hình ảnh không cần thiết
- Compress PDF

### ❌ Lỗi: "Question X failed validation"

**Nguyên nhân:**
- Câu hỏi thiếu thông tin
- Đáp án không hợp lệ
- Format không đúng

**Giải pháp:**
- Xem lại dòng đó trong PDF
- Đảm bảo có đầy đủ 4 đáp án
- Đảm bảo đáp án là A, B, C, hoặc D

---

## 💡 Ví Dụ Thực Tế

### Toán Học
```
1. 5 × 8 bằng bao nhiêu?
A) 35
B) 40
C) 45
D) 50
Answer: B

2. Căn bậc hai của 144 là bao nhiêu?
A) 10
B) 11
C) 12
D) 13
Answer: C
```

### Lịch Sử
```
1. Thế chiến thứ II kết thúc vào năm nào?
A) 1943
B) 1944
C) 1945
D) 1946
Answer: C

2. Ai là nhà lãnh đạo Đức trong Thế chiến II?
A) Kaiser Wilhelm II
B) Adolf Hitler
C) Otto von Bismarck
D) Frederick the Great
Answer: B
```

### Khoa Học
```
1. Quá trình quang hợp của cây sản xuất ra gì?
A) Nước
B) Dioxide carbonic
C) Oxygen
D) Nitrogen
Answer: C

2. Phần nào của tế bào là trung tâm của tế bào?
A) Tế bào chất
B) Nhân
C) Ty thể
D) Lạp thể
Answer: B
```

---

## 🎓 Thủ Thuật

### 1. Quy Đổi Từ Word Sang PDF
- Mở tài liệu Word
- **File** → **Export As** → **Export As PDF**

### 2. Quy Đổi Từ Google Docs
- Mở tài liệu
- **File** → **Download** → **PDF Document (.pdf)**

### 3. Quy Đổi Từ Excel
- Tạo file Excel với các cột: Q1, Q2, Q3, ... (câu hỏi)
- Nội dung các cell theo format
- Dùng macro hoặc công cụ tạo PDF từ Excel

---

## 📞 Hỗ Trợ

Nếu gặp vấn đề với import PDF:
1. Kiểm tra lại format
2. Xem phần "Lỗi Phổ Biến" trên
3. Liên hệ admin hoặc technical team

**Happy importing!** 🎉
