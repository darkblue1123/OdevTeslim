using AutoMapper;
using OdevTeslim.DTOs;
using OdevTeslim.Models;


namespace Uyg.API.Mapping
{
    public class MapProfile : Profile
    {
       public MapProfile() 
        {
            CreateMap<Submission,SubmissionDto>().ReverseMap();
            CreateMap<Course,CourseDto>().ReverseMap();
            CreateMap<AppUser,UserDto>().ReverseMap();
            CreateMap<CourseEnrollment,EnrollmentDto>().ReverseMap();
            CreateMap<Assignment,AssignmentDto>().ReverseMap();
        }
    }
}
