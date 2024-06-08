using LinuxLearner.Domain;

namespace LinuxLearner.Features.Courses;

public class CourseService(CourseRepository courseRepository)
{
    public async Task<CourseDto> CreateCourseAsync(CourseCreateDto courseCreateDto)
    {
        var course = MapToCourse(courseCreateDto);
        await courseRepository.AddCourseAsync(course);
        return MapToCourseDto(course);
    }

    public async Task<CourseDto?> GetCourseAsync(Guid id)
    {
        var course = await courseRepository.GetCourseAsync(id);
        return course is null ? null : MapToCourseDto(course);
    }

    public async Task<bool> PatchCourseAsync(Guid id, CoursePatchDto coursePatchDto)
    {
        var course = await courseRepository.GetCourseAsync(id);
        if (course is null) return false;
        
        ProjectCoursePatchDto(course, coursePatchDto);
        await courseRepository.UpdateCourseAsync(course);

        return true;
    }
    
    private static CourseDto MapToCourseDto(Course course) =>
        new(course.Id, course.Name, course.Description, course.AcceptanceMode);

    private static Course MapToCourse(CourseCreateDto courseCreateDto) => new()
    {
        Name = courseCreateDto.Name,
        Description = courseCreateDto.Description,
        AcceptanceMode = courseCreateDto.AcceptanceMode
    };

    private static void ProjectCoursePatchDto(Course course, CoursePatchDto coursePatchDto)
    {
        if (coursePatchDto.Name is not null) course.Name = coursePatchDto.Name;
        course.Description = coursePatchDto.Description;
        if (coursePatchDto.AcceptanceMode.HasValue) course.AcceptanceMode = coursePatchDto.AcceptanceMode.Value;
    }
}