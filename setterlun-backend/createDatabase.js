// createDatabase.js
const mysql = require('mysql2/promise'); // Use mysql2 library
require('dotenv').config(); // Load .env variables

async function createDBAndTables() {
  try {
    // 1️⃣ Connect to MySQL server
    const connection = await mysql.createConnection({
      host: process.env.DB_HOST,
      user: process.env.DB_USER,
      password: process.env.DB_PASS
    });

    // 2️⃣ Create the database if it doesn't exist
    await connection.query(
      `CREATE DATABASE IF NOT EXISTS ${process.env.DB_NAME} CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;`
    );
    console.log(`Database ${process.env.DB_NAME} created or already exists`);

    // 3️⃣ Use the database
    await connection.query(`USE ${process.env.DB_NAME};`);

    // 4️⃣ Create all tables
    const tables = [];

    // Users table
    tables.push(`
      CREATE TABLE IF NOT EXISTS users (
        id BIGINT AUTO_INCREMENT PRIMARY KEY,
        full_name VARCHAR(255) NOT NULL,
        username VARCHAR(100) UNIQUE NOT NULL,
        email VARCHAR(255) UNIQUE NOT NULL,
        phone VARCHAR(50),
        timezone VARCHAR(50),
        discovery_source VARCHAR(100),
        referral_code VARCHAR(50),
        referred_by BIGINT,
        avatar_json JSON,
        joined_date DATETIME DEFAULT CURRENT_TIMESTAMP,
        last_login DATETIME,
        current_step INT DEFAULT 1,
        status ENUM('active','inactive','blocked') DEFAULT 'active',
        created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
        updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
        FOREIGN KEY (referred_by) REFERENCES users(id) ON DELETE SET NULL
      );
    `);

    // Companies table
    tables.push(`
      CREATE TABLE IF NOT EXISTS companies (
        id BIGINT AUTO_INCREMENT PRIMARY KEY,
        name VARCHAR(255),
        logo_url VARCHAR(500),
        owner_id BIGINT,
        created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
        updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
        FOREIGN KEY (owner_id) REFERENCES users(id) ON DELETE CASCADE
      );
    `);

    // Company Members
    tables.push(`
      CREATE TABLE IF NOT EXISTS company_members (
        id BIGINT AUTO_INCREMENT PRIMARY KEY,
        company_id BIGINT,
        user_id BIGINT,
        role ENUM('member','admin') DEFAULT 'member',
        joined_at DATETIME DEFAULT CURRENT_TIMESTAMP,
        FOREIGN KEY (company_id) REFERENCES companies(id) ON DELETE CASCADE,
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
      );
    `);

    // Personal Profiles
    tables.push(`
      CREATE TABLE IF NOT EXISTS personal_profiles (
        id BIGINT AUTO_INCREMENT PRIMARY KEY,
        user_id BIGINT UNIQUE,
        bio TEXT,
        email_visibility ENUM('public','private') DEFAULT 'private',
        phone_visibility ENUM('public','private') DEFAULT 'private',
        city VARCHAR(100),
        country VARCHAR(100),
        skills JSON,
        ads BOOLEAN DEFAULT FALSE,
        seo BOOLEAN DEFAULT FALSE,
        content_creation BOOLEAN DEFAULT FALSE,
        other_skills JSON,
        created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
        updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
      );
    `);

    // Business Profiles
    tables.push(`
      CREATE TABLE IF NOT EXISTS business_profiles (
        id BIGINT AUTO_INCREMENT PRIMARY KEY,
        user_id BIGINT UNIQUE,
        business_name VARCHAR(255),
        business_website VARCHAR(500),
        social_links JSON,
        monthly_revenue VARCHAR(50),
        business_type ENUM('Agency','Sales','Growth Operator','Consultant','Coach','Info/Community','SAAS','Local Business','Ecommerce','Freelancer','Other'),
        primary_model VARCHAR(255),
        products_services JSON,
        lead_sources JSON,
        sales_issues JSON,
        sales_process_status VARCHAR(100),
        fulfillment_challenges JSON,
        tools_used JSON,
        authority_level ENUM('beginner','intermediate','advanced'),
        active_authority_building BOOLEAN DEFAULT FALSE,
        created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
        updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
      );
    `);

    // Onboarding Steps
    tables.push(`
      CREATE TABLE IF NOT EXISTS onboarding_steps (
        id BIGINT AUTO_INCREMENT PRIMARY KEY,
        user_id BIGINT,
        step_number INT,
        completed BOOLEAN DEFAULT FALSE,
        completed_at DATETIME,
        data_json JSON,
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
      );
    `);

    // Bricks
    tables.push(`
      CREATE TABLE IF NOT EXISTS bricks (
        id BIGINT AUTO_INCREMENT PRIMARY KEY,
        user_id BIGINT,
        name_on_brick VARCHAR(255),
        business_name VARCHAR(255),
        message TEXT,
        brick_position JSON,
        created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
      );
    `);

    // Courses
    tables.push(`
      CREATE TABLE IF NOT EXISTS courses (
        id BIGINT AUTO_INCREMENT PRIMARY KEY,
        name VARCHAR(255),
        description TEXT,
        video_link VARCHAR(500),
        recommended_for JSON,
        universal BOOLEAN DEFAULT FALSE,
        created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
        updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
      );
    `);

    // User Course Assignments
    tables.push(`
      CREATE TABLE IF NOT EXISTS user_course_assignments (
        id BIGINT AUTO_INCREMENT PRIMARY KEY,
        user_id BIGINT,
        course_id BIGINT,
        status ENUM('locked','unlocked','in_progress','completed') DEFAULT 'locked',
        progress FLOAT DEFAULT 0,
        assigned_at DATETIME DEFAULT CURRENT_TIMESTAMP,
        completed_at DATETIME,
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
        FOREIGN KEY (course_id) REFERENCES courses(id) ON DELETE CASCADE
      );
    `);

    // User Avatars (Separate table for extensibility)
    tables.push(`
      CREATE TABLE IF NOT EXISTS user_avatars (
        id BIGINT AUTO_INCREMENT PRIMARY KEY,
        user_id BIGINT UNIQUE,
        avatar_index INT DEFAULT 0,
        hair_color_index INT DEFAULT 0,
        hair_style_index INT DEFAULT 0,
        outfit_index INT DEFAULT 0,
        created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
        updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
      );
    `);

    // Time Capsules
    tables.push(`
      CREATE TABLE IF NOT EXISTS time_capsules (
        id BIGINT AUTO_INCREMENT PRIMARY KEY,
        user_id BIGINT,
        message TEXT,
        lock_until DATETIME,
        created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
      );
    `);

    // Execute table creation queries
    for (let sql of tables) {
      await connection.query(sql);
      console.log('Table created successfully');
    }

    await connection.end();
    console.log('All done! Database and tables are ready.');
  } catch (err) {
    console.error('Error creating database or tables:', err);
  }
}

// Run the function
createDBAndTables();