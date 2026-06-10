# 🚀 CVAnalyzer — AI-Powered Smart ATS & Semantic Match Engine

CVAnalyzer, geleneksel kelime eşleştirme (keyword matching) mantığını bir kenara bırakıp **Yapay Zeka (Google Gemini 3.1 Flash Lite)** ve **Vektörel Veritabanı (PgVector)** kullanarak adayları "anlamsal" olarak eşleştiren yeni nesil bir Aday Takip Sistemidir (ATS).

Sıradan sistemlerin aksine "React.js" yazan bir adayı "React" aramasında bulabilir, ancak "C" bilen bir adayı "React" aramasında eşleştirmez. Ayrıca adayın hangi yeteneği tam olarak kaç yıl kullandığını saniyesinde hesaplayarak **Yetenek Bazlı Deneyim Puanlaması** yapar.

---

## 🔥 Temel Özellikler (Enterprise V3)

- **🧠 Semantik Analiz:** PDF formatında yüklenen CV'ler, Gemini AI tarafından okunarak yapılandırılmış JSON verisine dönüştürülür ve vektör uzayına aktarılır.
- **🚀 Çift Motorlu Vektör Arama:** Adaylar sadece genel metin özetleriyle değil, özel olarak ayrıştırılmış **"Yetenek Vektörleri"** ile aranır. Uzaklık hesaplamaları (Cosine Distance) PostgreSQL üzerinde yapılır.
- **🛡️ Kelime Sınırı (Word Boundary) Koruması:** C#, C++ gibi noktalama içeren teknolojiler ve kısa diller (Go, R, C) için özel geliştirilmiş esnek eşleşme algoritması.
- **⏱️ Yetenek Bazlı Deneyim:** Adayın toplam çalışma süresine değil, **"Aranan yeteneği geçmişinde kaç yıl kullandığına"** bakar. (Örn: "En az 3 yıl Docker tecrübesi olan aday").
- **⚖️ Gerçekçi Puanlama:** Adaylara %100 üzerinden abartılı skorlar vermek yerine (Skor Enflasyonu), anlamsal uzaklık (Max %60) ve kesin filtreleri (Max %40) harmanlayarak mantıklı ve rekabetçi bir eşleşme puanı sunar.

---

## 🛠️ Teknoloji Yığını (Tech Stack)

- **Backend:** .NET 8, ASP.NET Core MVC, C#
- **ORM:** Entity Framework Core
- **Veritabanı:** PostgreSQL + PgVector (Vektör uzayı için)
- **Yapay Zeka:** Google Gemini API (v1beta)
- **Altyapı:** Docker & Docker Compose
- **Frontend:** HTML5, CSS3, Bootstrap 5, Javascript

---

## ⚙️ Kurulum & Geliştirme Ortamı

Projeyi kendi bilgisayarınızda çalıştırmak için aşağıdaki adımları izleyin:

### Gereksinimler
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (PostgreSQL ve PgVector için zorunludur)

### 1. Projeyi Klonlayın
```bash
git clone https://github.com/kullaniciadi/CVAnalyzer.git
cd CVAnalyzer
```

### 2. Veritabanını Ayağa Kaldırın (Docker)
Projede hazır bir `docker-compose.yml` bulunmaktadır. Terminalde şu komutu çalıştırarak PostgreSQL'i (ve PgVector eklentisini) başlatın:
```bash
docker-compose up -d
```

### 3. Yapay Zeka (Gemini) API Anahtarını Ekleyin
Sistemin CV'leri analiz edebilmesi için bir Google Gemini API anahtarına ihtiyacınız var. Güvenlik gereği bunu `.NET User Secrets` kullanarak ekleyin:
```bash
dotnet user-secrets init
dotnet user-secrets set "GeminiApiKey" "BURAYA_API_ANAHTARINIZI_YAZIN"
```

### 4. Veritabanı Tablolarını Oluşturun (Migration)
```bash
dotnet ef database update
```

### 5. Projeyi Başlatın
```bash
dotnet run
```
Uygulama başarıyla başladığında `http://localhost:5000` (veya 5001) adresi üzerinden sisteme erişebilir ve adayların CV'lerini yükleyerek yapay zeka analizini test etmeye başlayabilirsiniz!

---

## 🛡️ Katkıda Bulunma ve Lisans
Bu proje yapay zeka destekli insan kaynakları süreçlerini optimize etmek amacıyla geliştirilmiştir. Geliştirmelere ve Pull Request'lere açıktır. Mimaride yapılacak değişikliklerde PgVector uyumluluğunu göz önünde bulundurunuz.
