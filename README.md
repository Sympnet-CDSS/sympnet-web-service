# 🩺 SympNet - AI-Powered Telemedicine & Clinical Decision Support System

SympNet is a comprehensive, multi-platform telemedicine ecosystem designed to bridge the gap between patients and medical professionals. By leveraging advanced Artificial Intelligence (LLMs and Retrieval-Augmented Generation), SympNet acts as a powerful co-pilot for doctors while providing a seamless healthcare journey for patients.

This project was built as an academic milestone, demonstrating proficiency in microservices architecture, modern web/mobile development, and practical AI integration in the healthcare domain.

---

## 🌟 Key Features

### 🧑‍⚕️ For Medical Professionals (Web Dashboard)

- **AI-Assisted Diagnostics:** Analyzes patient symptoms (text or voice) to generate differential diagnostic hypotheses with Explainable AI (XAI) insights.
- **Intelligent Prescription Validation:** Automatically cross-references prescriptions with the patient's medical history, known allergies, and clinical guidelines (PubMed) to prevent dangerous drug interactions or illogical treatments.
- **Appointment Management:** Secure dashboard to manage patient records, daily schedules, and consultation notes.

### 🤒 For Patients (Mobile App)

- **Symptom Checker & Chatbot:** Patients can report their symptoms conversationally to receive preliminary guidance.
- **Doctor Discovery & Booking:** Find nearby specialists based on the AI's specialty recommendation and book appointments.
- **Health Record Access:** View past prescriptions and manage personal medical history (allergies, chronic diseases).

---

## 🏗️ System Architecture & Tech Stack

The system is divided into three main microservices:

1.  **AI Diagnostic Microservice (Python)**
    - **Framework:** FastAPI
    - **AI Models:** Groq LLM (Llama 3), Whisper (Voice-to-Text)
    - **Data & Vector Search:** pgvector (PostgreSQL) for RAG (Retrieval-Augmented Generation) based on medical literature.
    - **Agents:** Symptom Agent, Hypothesis Agent, Validator Agent, Explainable AI (XAI) Agent.

2.  **Backend Web API & Dashboard (.NET)**
    - **Framework:** C#, ASP.NET Core 8, Blazor WebAssembly
    - **ORM & DB:** Entity Framework Core, PostgreSQL
    - **Features:** JWT Authentication, SignalR (WebSockets) for real-time notifications, RESTful API.

3.  **Mobile Application (Android)**
    - **Language:** Java
    - **Networking:** Retrofit & OkHttp
    - **Architecture:** Native Android components with secure API integration.

---

## 🚀 How to Run the Project Locally

To run the full SympNet ecosystem on your local machine, you need to start the three services simultaneously.

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [Python 3.10+](https://www.python.org/downloads/)
- [Android Studio](https://developer.android.com/studio)
- PostgreSQL running locally (Port 5432)

### 1. Start the AI Microservice (Python)

```bash
cd sympnet-ai
pip install -r requirements.txt
uvicorn app.main:app --port 8001
```

The AI service will run at http://localhost:8001

### 2. Start the Backend Web API (.NET)

```bash
cd sympnet-web-service/SympNet.WebApi
dotnet run
```

The API will run at http://localhost:5057

### 3. Start the Web Dashboard (.NET)

```bash
cd sympnet-web-service/SympNet.WebDashboard
dotnet run
```

The Dashboard will run at http://localhost:5002 (or the port specified in launchSettings)

(Note: For the Android app to connect to the local API on a physical device, use ngrok to tunnel port 5057).

## 🛡️ Security & Privacy

Data Encryption: Passwords are encrypted using BCrypt.
Authentication: Secure JWT-based authentication for both the mobile app and web dashboard.
Clinical Safety: AI is strictly constrained to act as a Decision Support System (DSS), ensuring the final medical decision is always made by a certified physician.

## 👥 Authors

- **Sirine Rezgui** - _Software Engineering Student_ - [LinkedIn](www.linkedin.com/in/sirine-rezgui)

- **Yasmine Ouertani** - _Software Engineering Student_ - [LinkedIn](www.linkedin.com/in/yasmine-ouertani)
