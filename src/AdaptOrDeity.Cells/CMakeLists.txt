# The game's cell model in libcuflynx's generated C (generated/, written by tools/scripts/build_cell_library.py; never
# edited): a shared library aod_cell for the managed simulation (Unity's Mono, tests) and a static aod_cell_static that
# the NativeAOT simulation library links in. Both land in src/AdaptOrDeity.Native/bin/cells.
#   cmake -S src/AdaptOrDeity.Cells -B build/cells && cmake --build build/cells --config Release
cmake_minimum_required(VERSION 3.13)
project(aod_cell C)

if(NOT CMAKE_BUILD_TYPE AND NOT CMAKE_CONFIGURATION_TYPES)
    set(CMAKE_BUILD_TYPE Release)
endif()

set(SOURCES generated/model0d_core.c generated/batched_kernels.c generated/batched_runtime.c)
set(OUT ${CMAKE_CURRENT_SOURCE_DIR}/../AdaptOrDeity.Native/bin/cells)
# libCellML's part runs only for initial values, computed constants and on-demand values; the kernels are the hot path.
set_source_files_properties(generated/model0d_core.c PROPERTIES COMPILE_OPTIONS "$<IF:$<C_COMPILER_ID:MSVC>,/O1,-O1>")

add_library(aod_cell SHARED ${SOURCES})
add_library(aod_cell_static STATIC ${SOURCES})
find_library(MATH_LIBRARY m)
foreach(target aod_cell aod_cell_static)
    set_target_properties(${target} PROPERTIES C_STANDARD 99 C_VISIBILITY_PRESET hidden POSITION_INDEPENDENT_CODE ON
        # the same folder for every configuration (no Release/ subfolder with Visual Studio)
        RUNTIME_OUTPUT_DIRECTORY $<1:${OUT}> LIBRARY_OUTPUT_DIRECTORY $<1:${OUT}> ARCHIVE_OUTPUT_DIRECTORY $<1:${OUT}>)
    target_compile_options(${target} PRIVATE $<$<C_COMPILER_ID:GNU,Clang,AppleClang>:-fno-math-errno>)
    if(MATH_LIBRARY)
        target_link_libraries(${target} PRIVATE ${MATH_LIBRARY})
    endif()
endforeach()
