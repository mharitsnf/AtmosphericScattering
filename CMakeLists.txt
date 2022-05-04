cmake_minimum_required(VERSION 3.0.0)
project(AtmosphericScattering VERSION 0.1.0)

include(CTest)
enable_testing()

add_executable(AtmosphericScattering main.cpp)

set(CPACK_PROJECT_NAME ${PROJECT_NAME})
set(CPACK_PROJECT_VERSION ${PROJECT_VERSION})
include(CPack)

find_package(Git QUIET)
if (GIT_FOUND AND EXISTS ${PROJECT_SOURCE_DIR}/.git)
    option(GIT_SUBMODULE "Check submodules during build" ON)
    if (GIT_SUBMODULE)
        message(STATUS "Submodule update")
        execute_process(COMMAND ${GIT_EXECUTABLE} submodule update --init --recursive
                        WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
                        RESULT_VARIABLE GIT_SUBMOD_RESULT)
        if(NOT GIT_SUBMOD_RESULT EQUAL "0")
            message(FATAL_ERROR "git submodule update --init failed with ${GIT_SUBMOD_RESULT}")
        endif()
    endif()
endif()

add_subdirectory(external/glfw)
add_subdirectory(external/glew)
add_subdirectory(external/glad)

target_include_directories(${PROJECT_NAME}
    PUBLIC external/glfw/include
    PUBLIC external/glew/include
    PUBLIC external/glad/include
)

target_link_directories(${PROJECT_NAME}
    PRIVATE external/glfw/src
    PRIVATE external/glew/src
    PRIVATE external/glad/src
)

target_link_libraries(${PROJECT_NAME} glfw libglew_static glad)