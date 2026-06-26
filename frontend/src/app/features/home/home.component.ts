import { Component, OnInit, AfterViewInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { JobService } from '../../core/services/job.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './home.component.html',
  styles: [`
    @keyframes blob {
      0%   { transform: translate(0,0) scale(1); }
      33%  { transform: translate(30px,-50px) scale(1.12); }
      66%  { transform: translate(-20px,20px) scale(0.9); }
      100% { transform: translate(0,0) scale(1); }
    }
    @keyframes blob2 {
      0%   { transform: translate(0,0) scale(1); }
      33%  { transform: translate(-40px,40px) scale(1.15); }
      66%  { transform: translate(30px,-30px) scale(0.88); }
      100% { transform: translate(0,0) scale(1); }
    }
    @keyframes float {
      0%,100% { transform: translateY(0) rotate(0deg); }
      40%     { transform: translateY(-18px) rotate(1.5deg); }
      70%     { transform: translateY(-9px) rotate(-1deg); }
    }
    @keyframes floatB {
      0%,100% { transform: translateY(0) rotate(0deg); }
      35%     { transform: translateY(-12px) rotate(-1.5deg); }
      65%     { transform: translateY(-22px) rotate(1deg); }
    }
    @keyframes marquee {
      0%   { transform: translateX(0); }
      100% { transform: translateX(-50%); }
    }
    @keyframes fadeUp {
      from { opacity:0; transform:translateY(28px); }
      to   { opacity:1; transform:translateY(0); }
    }
    @keyframes slideLeft {
      from { opacity:0; transform:translateX(-40px); }
      to   { opacity:1; transform:translateX(0); }
    }
    @keyframes slideRight {
      from { opacity:0; transform:translateX(40px); }
      to   { opacity:1; transform:translateX(0); }
    }
    @keyframes ping2 {
      0%   { transform:scale(1); opacity:1; }
      100% { transform:scale(2.4); opacity:0; }
    }

    .blob-1 { animation: blob  8s  infinite ease-in-out; filter: blur(70px); }
    .blob-2 { animation: blob2 10s infinite ease-in-out; filter: blur(70px); }
    .blob-3 { animation: blob  12s infinite ease-in-out 3s; filter: blur(70px); }

    .float-a { animation: float  6s ease-in-out infinite; }
    .float-b { animation: floatB 8s ease-in-out infinite; }
    .float-c { animation: float  7s ease-in-out infinite 2s; }

    .marquee-wrap { overflow:hidden; }
    .marquee-track { display:flex; animation: marquee 32s linear infinite; }
    .marquee-track:hover { animation-play-state: paused; }

    .fu-0  { animation: fadeUp  0.55s ease both; }
    .fu-1  { animation: fadeUp  0.55s ease both 0.10s; }
    .fu-2  { animation: fadeUp  0.55s ease both 0.20s; }
    .fu-3  { animation: fadeUp  0.55s ease both 0.30s; }
    .fu-4  { animation: fadeUp  0.55s ease both 0.40s; }
    .sl    { animation: slideLeft  0.7s ease both 0.55s; }
    .sr    { animation: slideRight 0.7s ease both 0.60s; }

    .reveal {
      opacity:0; transform:translateY(24px);
      transition: opacity 0.65s ease, transform 0.65s ease;
    }
    .reveal.visible { opacity:1; transform:translateY(0); }

    .job-card {
      transition: transform 0.22s ease, box-shadow 0.22s ease;
      cursor: pointer;
    }
    .job-card:hover { transform:translateY(-7px); box-shadow:0 20px 48px rgba(0,0,0,0.13); }

    .feat-card {
      transition: transform 0.22s ease, box-shadow 0.22s ease, border-color 0.22s ease;
    }
    .feat-card:hover { transform:translateY(-5px); box-shadow:0 18px 40px rgba(59,130,246,0.14); border-color:#3b82f6; }

    .step-card { transition: transform 0.22s ease; }
    .step-card:hover { transform:translateY(-5px); }

    .grad-text {
      background: linear-gradient(135deg, #60a5fa 0%, #a78bfa 100%);
      -webkit-background-clip: text; -webkit-text-fill-color: transparent; background-clip: text;
    }
    .ping-dot::before {
      content:''; position:absolute; inset:0; border-radius:50%;
      background:currentColor; animation: ping2 1.6s ease-out infinite;
    }
    .search-bar input:focus { outline:none; box-shadow:none; }

    .tag-pill {
      display:inline-block; padding: 2px 10px; border-radius: 9999px;
      font-size: 0.7rem; font-weight: 600; letter-spacing: 0.03em;
    }
  `]
})
export class HomeComponent implements OnInit, AfterViewInit {
  private jobService = inject(JobService);
  private router    = inject(Router);

  featuredJobs = signal<any[]>([]);
  searchKeyword  = '';
  searchLocation = '';

  readonly stats = [
    { value: '500+',  label: 'Active Jobs',  icon: '💼' },
    { value: '200+',  label: 'Companies',    icon: '🏢' },
    { value: '10K+',  label: 'Candidates',   icon: '👥' },
    { value: '95%',   label: 'Success Rate', icon: '🎯' },
  ];

  readonly companiesRow = [
    'TechCorp', 'Infosys', 'Wipro', 'HCL Technologies',
    'Tata Consultancy', 'Flipkart', 'Amazon India', 'Google',
    'Microsoft', 'Accenture', 'Capgemini', 'IBM India',
  ];

  readonly steps = [
    { n:'01', icon:'👤', title:'Create Your Profile',
      desc:'Sign up in 30 seconds. Add skills, experience, and upload your resume to get discovered.' },
    { n:'02', icon:'🔍', title:'Browse & Apply',
      desc:'Search hundreds of verified openings. Filter by role, location, and salary range.' },
    { n:'03', icon:'🚀', title:'Get Hired',
      desc:'Track applications in real time. Get shortlisted, interview, and land your dream offer.' },
  ];

  readonly features = [
    { icon:'⚡', grad:'from-yellow-400 to-orange-500', bg:'bg-yellow-50', border:'border-yellow-100',
      title:'Instant Notifications',
      desc:'Real-time alerts whenever your application status changes. Never miss an HR update.' },
    { icon:'🔒', grad:'from-blue-400 to-indigo-600',   bg:'bg-blue-50',   border:'border-blue-100',
      title:'Private Resume Storage',
      desc:'Resumes stored in Azure Blob Storage with time-limited SAS URLs — only visible to HR.' },
    { icon:'🎯', grad:'from-green-400 to-teal-500',    bg:'bg-green-50',  border:'border-green-100',
      title:'Smart Keyword Matching',
      desc:'Our matching engine surfaces the most relevant candidates to every job posting.' },
    { icon:'📊', grad:'from-violet-400 to-purple-600', bg:'bg-violet-50', border:'border-violet-100',
      title:'Full Lifecycle Tracking',
      desc:'Submitted → Under Review → Shortlisted → Hired. Every step visible on your dashboard.' },
  ];

  readonly testimonials = [
    { name:'Priya Sharma', role:'Software Engineer', company:'TechCorp Solutions',
      text:'TalentBridge is the cleanest hiring portal I\'ve used. Applied, got shortlisted, and received an offer — all in 2 weeks!',
      avatar:'PS', color:'bg-blue-600' },
    { name:'Rohan Mehta', role:'HR Manager', company:'Innovate Labs',
      text:'As an HR, the dashboard is incredibly clean. I can review 50 applications in an hour and view resumes right in the browser.',
      avatar:'RM', color:'bg-violet-600' },
    { name:'Aditya Kulkarni', role:'DevOps Engineer', company:'CloudFirst',
      text:'Found a remote DevOps role through TalentBridge in under 3 days. The search filters are exactly what I needed.',
      avatar:'AK', color:'bg-teal-600' },
  ];

  ngOnInit() {
    this.jobService.searchJobs('', '', 1, 6).subscribe({
      next: r  => this.featuredJobs.set(r.items.slice(0, 6)),
      error: () => {}
    });
  }

  ngAfterViewInit() {
    const io = new IntersectionObserver(
      entries => entries.forEach(e => {
        if (e.isIntersecting) { e.target.classList.add('visible'); io.unobserve(e.target); }
      }),
      { threshold: 0.12 }
    );
    document.querySelectorAll('.reveal').forEach(el => io.observe(el));
  }

  search() {
    this.router.navigate(['/jobs'], { queryParams: {
      keyword: this.searchKeyword  || undefined,
      location: this.searchLocation || undefined,
    }});
  }

  fmt(v: number): string {
    return v >= 100000 ? `$${(v/1000).toFixed(0)}K` : `$${v.toLocaleString()}`;
  }

  jobTypeLabel(t: string): string {
    const map: Record<string,string> = { FullTime:'Full-Time', PartTime:'Part-Time', Contract:'Contract', Internship:'Internship' };
    return map[t] ?? t ?? 'Full-Time';
  }
}
