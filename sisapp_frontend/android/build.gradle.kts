import com.android.build.gradle.LibraryExtension
allprojects {
    repositories {
        google()
        mavenCentral()
    }
}

val newBuildDir: Directory = rootProject.layout.buildDirectory.dir("../../build").get()
rootProject.layout.buildDirectory.value(newBuildDir)

subprojects {
    val newSubprojectBuildDir: Directory = newBuildDir.dir(project.name)
    project.layout.buildDirectory.value(newSubprojectBuildDir)
}
subprojects {
    project.evaluationDependsOn(":app")
}

tasks.register<Delete>("clean") {
    delete(rootProject.layout.buildDirectory)
}
subprojects {
    plugins.withId("com.android.library") {
        extensions.configure<LibraryExtension>("android") {
            if (namespace == null) {
                namespace = "fix.${project.name.replace('-', '_')}"
            }
        }
    }
}

subprojects {
    if (name.contains("paypal")) {
        val manifestFile = file("src/main/AndroidManifest.xml")
        if (manifestFile.exists()) {
            val original = manifestFile.readText()
            val patched = original.replace(Regex("""\s+package="[^"]*""""), "")
            if (patched != original) {
                manifestFile.writeText(patched)
            }
        }
    }
}
