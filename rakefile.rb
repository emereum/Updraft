PROJECT_NAME = "Updraft"
CONFIGURATION = "Release"

PROJECT_ROOT_DIR = File.dirname(__FILE__)
SRC_DIR = File.join(PROJECT_ROOT_DIR, "src")
PUBLISH_DIR = File.join(PROJECT_ROOT_DIR, "publish")

desc "Compiles #{PROJECT_NAME}."
task :build do
    Dir.chdir(SRC_DIR) do
        # Restore nuget packages
        sh "nuget restore #{PROJECT_NAME}.sln"
        
        # Build with MSBuild to generate project.fragment.lock.json
        sh "\"C:/Program Files (x86)/MSBuild/14.0/Bin/msbuild.exe\" #{PROJECT_NAME}.sln /p:Configuration=\"" + CONFIGURATION + "\""
    end
end

task :pack => [:build] do
    rm_rf PUBLISH_DIR
    cp_r File.join(SRC_DIR, "Updraft", "bin", CONFIGURATION), PUBLISH_DIR
    
    Dir.chdir(PUBLISH_DIR) do
        sh "7z a -r updraft.zip * -x!*.xml"
    end
end