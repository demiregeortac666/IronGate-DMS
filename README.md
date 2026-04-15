# Dormitory Management System (DMS 2026)

Bu proje, bir öğrenci yurdunun yönetim süreçlerini kolaylaştırmak, gelir/giderleri takip etmek ve öğrenci-oda kayıtlarını dijital ortamda yönetmek amacıyla geliştirilmiş kapsamlı bir **SaaS (Software as a Service)** web uygulamasıdır.

## 🚀 Teknolojiler
- **Backend:** C#, ASP.NET Core MVC (8.0/7.0 uyumlu)
- **Veritabanı:** SQLite & Entity Framework Core (Code-First)
- **Frontend:** HTML5, CSS3, Bootstrap 5, Chart.js, Bootstrap Icons
- **Mimari:** Model-View-Controller (MVC)

## 🛠️ Nasıl Çalıştırılır?
Projeyi sıfırdan derlemek ve başlatmak için aşağıdaki adımları kullanabilirsiniz:

1. Proje dizininde terminali açın:
   ```bash
   dotnet clean
   dotnet restore
   dotnet build
   ```
2. Veritabanının sıfırdan oluşturulması için:
   ```bash
   dotnet ef database update
   ```
3. Projeyi çalıştırın:
   ```bash
   dotnet run
   ```

> **Not:** Sistem varsayılan olarak `dormitory.db` adında bir SQLite dosyası oluşturur ve ilk çalıştırmada gerekli örnek verileri (seed data) içeriye doldurur.

## 👥 Varsayılan Kullanıcılar (Demo Test İçin)
Aşağıdaki kullanıcılarla sisteme giriş yapabilirsiniz. Şifrelerin tamamı: **Admin123!**
- **Admin Hesabı:** username: `admin` | role: Admin
- **Personel Hesabı:** username: `staff` | role: Staff
- **Öğrenci Hesabı:** username: `s.yilmaz` | role: Student

## ✨ Özellikler

### Admin Özellikleri
- Tüm kullanıcıları ve rolleri yönetme.
- Silinemez log kayıtları aracılığıyla denetim (Audit Logs).
- Sistem ayarlarını değiştirme.

### Staff (Personel) Özellikleri
- Öğrenci kayıt kabulü ve oda ataması (Kapasite kontrollü).
- Fatura (Invoice) ve tahsilat/ödeme (Payment) işleme (Mükerrer ve fazla ödeme engellidir).
- Öğrenci belgelerinin güvenli bir şekilde sisteme yüklenmesi ve barındırılması.

### Student (Öğrenci) Özellikleri
- Kendisine ait ödemeleri ve fatura geçmişini inceleme.
- Bakım & Onarım (Maintenance) talebi oluşturma.

### Dashboard & Raporlar (Reports)
- Yurdun doluluk oranının, açık/kapalı genel maliyetlerin Chart.js bar, doughnut ve line grafikleri yardımıyla görselleştirilmesi.
- Aylık kazanç analizleri, ödenmemiş faturalar raporu ve öğrenci borç tabloları.

## 💡 Eklenen Ekstra Geliştirmeler
1. **Veri Bütünlüğü ve Kısıtlamalar:**
   - Aynı dönem içinde çift fatura kesilmesi engellendi.
   - Tanımlı borcu aşan ödeme/tahsilat (overpayment) senaryoları engellendi.
   - Her `Student` başına yalnızca bir sistem kullanıcısı hesabı açılabilmesi için eşsizlik zincirleri kuruldu (Unique Constraints).
2. **Kullanıcı Deneyimi:**
   - İşlem başarı / başarısızlık durumları tüm CRUD sayfalarına (Create/Read/Update/Delete) `TempData` Alert entegrasyonuyla yansıtıldı.
   - Hata fırlatan kayıt silme durumlarında uygulama çökmesi yerine yakalanan açık ve net mesajlar eklendi (Örn: "Öğrenciye ait ödemeler olduğu için önce ödemeler silinmelidir.").
3. **Güvenli Dosya Yönetimi:**
   - Siber güvenliği sağlamak amacıyla dosya yüklemelerinde `Guid` üretilerek gerçek isimler maskelendi. Öğrenci hesabı tamamen silinirken, sunucudaki yüklenmiş fiziksel dosyanın da silinmesi programlandı.
