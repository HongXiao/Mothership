﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mothership;

public class IAIBase : MonoBehaviour 
{
    public enum ETeam
    {
        TEAM_NONE,
        TEAM_RED,
        TEAM_BLUE,
    }

    private static CPowerUpSO m_ItemsResource = null;
    public static CPowerUpSO ItemsResource { get { return m_ItemsResource; } set { m_ItemsResource = value; } }

    // A list holding all active red NPCs.
    protected static List< GameObject > m_liActiveReds = new List< GameObject >();

    // A list holding all active blue NPCs.
    protected static List< GameObject > m_liActiveBlues = new List< GameObject >();

    [ SerializeField ]
    protected ETeam m_eTeam = ETeam.TEAM_NONE;
    public ETeam Team { get { return m_eTeam; } }

    [ SerializeField ]
    protected GameObject m_goHomeBase = null;
    public GameObject HomeBase { get { return m_goHomeBase; } }

    // The speed of this lovely drone.
    [ SerializeField ]
	protected float m_fSpeed;
    public float Speed { get { return m_fSpeed; } }

    // The speed multiplier.
    protected float m_fSpeedMultiplier = 0;
    public float SpeedMultiplier { get { return m_fSpeedMultiplier; } }

    // Rotation speed.
    [ SerializeField ]
    protected float m_fRotationSpeed;
    public float RotationSpeed { get { return m_fRotationSpeed; } }

    // The Health.
    [ SerializeField ]
    protected float m_fHealth = 100;
    public float Health { get { return m_fHealth; } }

    [ SerializeField ]
	protected bool m_bShowPath = false;

    // Current target position.
    [ SerializeField ]
	protected Vector3 m_v3Target = Vector3.zero;
    public Vector3 TargetPosition { get { return m_v3Target; } }

    // We're going to hold a reference to the "gun" gameobject
    //  from which we're going to fire.
    [ SerializeField ]
    protected GameObject m_goGun;
	
    // Will indicate if we reached the target node.
	protected bool m_bReachedNode = true;
    public bool ReachedNode { get { return m_bReachedNode; } }

    protected bool m_bReachedTarget = false;
    public bool ReachedTarget { get { return m_bReachedTarget; } }

    // Will flag that this NPC is being attacked.
    protected bool m_bIsBeingAttacked = false;

    protected bool m_bTargetInRange = false;

    // Will hold ammo variables.
    protected Dictionary< string, uint > m_dictInventory = new Dictionary< string, uint >();

    // Will hold a reference to the prefabs we want to instantiate.
    protected Dictionary< string, GameObject > m_dictProjectilePrefabs = new Dictionary< string, GameObject >(); 

    // Will hold a handle on this object's animator
    protected Animator m_anAnimator;

    protected Vector3 m_v3CurrNode;
	protected int m_iNodeIndex;
	protected List< Vector3 > m_liPath = new List<Vector3>();
	protected float m_fOldTime = 0;
	protected float m_fCheckTime = 0;
	protected float m_fElapsedTime = 0;

    bool m_bCanFireBullet = true;
    bool m_bCanFireMissile = true;
    bool m_bCanFireRay = true;

    /////////////////////////////////////////////////////////////////////////////
    /// Function:               Start
    /////////////////////////////////////////////////////////////////////////////
    protected void Start()
    {
        // Error reporting.
        string strFunction = "IAIBase::Start()";

        if ( null == m_ItemsResource )
        {
            m_ItemsResource = Resources.Load< CPowerUpSO >( ResourcePacks.RESOURCE_CONTAINER_ITEMS );
            if ( null == m_ItemsResource )
            {
                Debug.LogError( string.Format("{0} {1} " + ErrorStrings.ERROR_AUDIO_FAILED_RELOAD, strFunction, ResourcePacks.RESOURCE_CONTAINER_ITEMS ) );
                return;
            }
        }

        // Add this NPC to the correct list depending on its team.
        switch ( m_eTeam )
        {
            case ETeam.TEAM_BLUE:

                m_liActiveBlues.Add( gameObject );

                break;

            case ETeam.TEAM_RED:

                m_liActiveReds.Add( gameObject );

                break;
            default:

                // Unassigned NPC detected, report the issue.
                Debug.LogError( string.Format( "{0} {1}: {2}", strFunction, ErrorStrings.ERROR_UNASSIGNED_NPC, transform.position ) );

                return;
        }

        // Initialize inventory stock.
        m_dictInventory.Add( Names.NAME_BULLET, 500 );
        m_dictInventory.Add( Names.NAME_MISSILE, 0 );
        m_dictInventory.Add( Names.NAME_RAY, 0 );

        // Get a handle on the NPC's animator.
        m_anAnimator = gameObject.GetComponent< Animator >();
        if ( null == m_anAnimator )
        {
            // Shit happened
            Debug.LogError( string.Format( "{0} {1}: {2}", strFunction, ErrorStrings.ERROR_MISSING_COMPONENT, typeof( Animator ).ToString() ) );
        }
    }

    /////////////////////////////////////////////////////////////////////////////
    /// Function:               Update
    /////////////////////////////////////////////////////////////////////////////
    protected void Update()
    {
        // For error reporting.
        string strFunction = "IAIBase::Update()";

        // Update the speeds.
        m_fSpeed = Time.deltaTime * m_fSpeedMultiplier;
        m_fRotationSpeed = Time.deltaTime * m_fSpeedMultiplier;

        // Ensure that this NPC knows where his homebase is according to his team.
        if ( null == m_goHomeBase )
        {
            string strBaseName;

            switch ( m_eTeam )
            {
                case ETeam.TEAM_BLUE:

                    strBaseName = Names.NAME_BLUE_BASE;
                    
                    break;
                case ETeam.TEAM_RED:

                    strBaseName = Names.NAME_RED_BASE;

                    break;
                default:

                    // This NPC doesn't have a valid team assigned, report the issue.
                    Debug.LogError( string.Format( "{0} {1}", strFunction, ErrorStrings.ERROR_UNASSIGNED_NPC ) );
                    return;
            }

            // Attempt to assign the homebase for this NPC.
            m_goHomeBase = GameObject.Find( strBaseName );
            if ( null == m_goHomeBase )
            {
                // We didn't manage to find the base, report the issue.
                Debug.LogError( string.Format( "{0} {1}: {2}", strFunction, ErrorStrings.ERROR_NULL_OBJECT, strBaseName ) );
                return;
            }

        }
    }

    /////////////////////////////////////////////////////////////////////////////
    /// Function:               GoTo
    /////////////////////////////////////////////////////////////////////////////
	protected void GoTo()
	{
        if ( m_v3Target == Vector3.zero )
            return;

        // Check if we want to show this character's path in the editor.
		if ( true == m_bShowPath )
		{
			for ( int i = 0; i < m_liPath.Count - 1; ++i )
			{
				Debug.DrawLine( ( Vector3 )m_liPath[ i ], ( Vector3 )m_liPath[ i + 1 ], Color.white, 0.01f );
			}
		}
		
		Vector3 v3NewPos = transform.position;
        float fDistance = 1f;

		if ( Vector3.Distance( transform.position, m_v3CurrNode ) < fDistance && m_v3CurrNode != m_v3Target )
		{
			m_iNodeIndex++;
			m_bReachedNode = true;
		}
        else if ( Vector3.Distance( transform.position, m_v3CurrNode ) < fDistance && m_v3CurrNode == m_v3Target )
        {
            m_bReachedTarget = true;
        }

		Vector3 v3Motion = m_v3CurrNode - v3NewPos;
		v3Motion.Normalize();

        transform.rotation = Quaternion.Slerp( transform.rotation, Quaternion.LookRotation( v3Motion ), m_fRotationSpeed );

		v3NewPos += v3Motion * m_fSpeed;
		
		transform.position = v3NewPos;
	}
	
    /////////////////////////////////////////////////////////////////////////////
    /// Function:               SetTarget
    /////////////////////////////////////////////////////////////////////////////
	protected void SetTarget()
	{
		m_liPath = CNodeController.FindPath( transform.position, m_v3Target );
		m_iNodeIndex = 0;
	}
	
    /////////////////////////////////////////////////////////////////////////////
    /// Function:               MoveOrder
    /////////////////////////////////////////////////////////////////////////////
	protected void MoveOrder( Vector3 v3Pos )
	{
		m_v3Target = v3Pos;
		SetTarget();
	}

    /////////////////////////////////////////////////////////////////////////////
    /// Function:               SetTeam
    /////////////////////////////////////////////////////////////////////////////
    protected void SetTeam( ETeam eTeam )
    {
        // Set the team for this NPC.
        m_eTeam = eTeam;
    }

    /////////////////////////////////////////////////////////////////////////////
    /// Function:               OnTriggerEnter
    /////////////////////////////////////////////////////////////////////////////
    protected void OnTriggerEnter( Collider cCollider ) 
    {
        string strFunction = "IAIBase::OnCollisionEnter()";

        // Get a handle on the gameObject with which we collided. 
        GameObject goObject = cCollider.gameObject;

        // Depending on the name of the object, react accordingly.
        switch ( goObject.tag )
        {
            case Tags.TAG_WEAPON:

                // Reduce the NPCs health depending on the type of projectile.
                if ( goObject.name == Names.NAME_MISSILE )
                    m_fHealth -= Constants.PROJECTILE_DAMAGE_MISSILE;

                else if ( goObject.name == Names.NAME_BULLET )
                    m_fHealth -= Constants.PROJECTILE_DAMAGE_BULLET;

                else if ( goObject.name == Names.NAME_RAY )
                    m_fHealth -= Constants.PROJECTILE_DAMAGE_RAY;

                // We've been attacked by an enemy, flag this fact.
                m_bIsBeingAttacked = true;
                
                break;

            case Tags.TAG_POWERUP:

                // Attempt to get a handle on the object's powerup script.
                CPowerUp cPowerUp = goObject.GetComponent< CPowerUp >();
                if ( null == cPowerUp )
                {
                    // We failed to get a handle, this shouldn't happen - report the error.
                    Debug.LogError( string.Format( "{0} {1}: {2}", strFunction, ErrorStrings.ERROR_MISSING_COMPONENT, typeof( CPowerUp ).ToString() ) );
                    return;
                }

                // Increase inventory stocks or health depending on the type of powerup.
                if ( goObject.name == Names.NAME_MISSILE )
                    m_dictInventory[ Names.NAME_MISSILE ] += 3;

                else if ( goObject.name == Names.NAME_BULLET )
                    m_dictInventory[ Names.NAME_MISSILE ] += 50;

                else if ( goObject.name == Names.NAME_RAY )
                    m_dictInventory[ Names.NAME_RAY ] += 1;

                else if ( goObject.name == Names.NAME_HEALTH )
                    m_fHealth += 50;

                cPowerUp.PickupPowerUp();

                break;
        }
    }

    /////////////////////////////////////////////////////////////////////////////
    /// Function:               GetClosestEnemy
    /////////////////////////////////////////////////////////////////////////////
    protected GameObject GetClosestEnemy()
    {
        // Loop through the enemy list and get a reference to the closest enemy.
        List< GameObject > liEnemies = new List< GameObject >();

        if ( m_eTeam == ETeam.TEAM_BLUE )
        {
            liEnemies = m_liActiveReds;
        }

        else if ( m_eTeam == ETeam.TEAM_RED )
        {
            liEnemies = m_liActiveBlues;
        }

        float fLowestDistance = -1;
        GameObject goClosestEnemy = null;

        foreach ( GameObject goEnemy in liEnemies )
        {
            float fDistance = Vector3.Distance( goEnemy.transform.position, transform.position );
            // Check if the variable hasn't been set yet.
            if ( fLowestDistance == -1 )
            { 
                fLowestDistance = fDistance ;
                goClosestEnemy = goEnemy;
                continue;
            }

            // Current enemy is closer than previous enemies.
            if ( fLowestDistance > fDistance )
            {
                fLowestDistance = fDistance;
                goClosestEnemy = goEnemy;
            }
        }

        return goClosestEnemy;
    }

    /////////////////////////////////////////////////////////////////////////////
    /// Function:               AttackTarget
    /////////////////////////////////////////////////////////////////////////////
    protected IEnumerator AttackTarget( Transform trEnemy )
    {
        

        while ( true == m_bTargetInRange )
        {
            if ( m_dictInventory[ Names.NAME_BULLET ] > 0 && true == m_bCanFireBullet )
            { 
                m_bCanFireBullet = false;
                Fire( Names.NAME_BULLET, trEnemy );
                yield return new WaitForSeconds( Constants.PROJECTILE_DELAY_BULLET );
                m_bCanFireBullet = true;
            }

            if ( m_dictInventory[ Names.NAME_MISSILE ] > 0 && true == m_bCanFireMissile )
            { 
                m_bCanFireMissile = false;
                Fire( Names.NAME_MISSILE, trEnemy );
                yield return new WaitForSeconds( Constants.PROJECTILE_DELAY_MISSILE );
                m_bCanFireMissile = true;
            }

            if ( m_dictInventory[ Names.NAME_RAY ] > 0 && true == m_bCanFireRay )
            { 
                m_bCanFireRay = false;
                Fire( Names.NAME_RAY, trEnemy );
                yield return new WaitForSeconds( Constants.PROJECTILE_DELAY_RAY );
                m_bCanFireRay = true;
            }

            yield return null;
        }
    } 

    /////////////////////////////////////////////////////////////////////////////
    /// Function:               Fire
    /////////////////////////////////////////////////////////////////////////////
    protected void Fire( string strProjectileName, Transform trEnemy )
    {
        string strFunction = "IAIBase::Fire()";
        
        m_dictProjectilePrefabs = m_ItemsResource.Weapons;
        if ( false == m_dictProjectilePrefabs.ContainsKey( strProjectileName ) )
        {
            Debug.LogError( string.Format( "{0} {1}: {2}", strFunction, ErrorStrings.ERROR_UNRECOGNIZED_NAME, strProjectileName ) );
            return;
        }

        GameObject goProjectile = m_dictProjectilePrefabs[ strProjectileName ];
        if ( null == goProjectile )
        {
            Debug.LogError( string.Format( "{0} {1}: {2}", strFunction, ErrorStrings.ERROR_NULL_OBJECT, typeof( GameObject ).ToString() ) );
            return;
        }

        CProjectile cProjectile = goProjectile.GetComponent< CProjectile >();
        if ( null == cProjectile )
        {
            Debug.LogError( string.Format( "{0} {1}: {2}", strFunction, ErrorStrings.ERROR_MISSING_COMPONENT, typeof( CProjectile ).ToString() ) );
            return;
        }

        Vector3 v3Direction = trEnemy.position - transform.position;
        cProjectile.Direction = v3Direction.normalized;
        cProjectile.Instantiator = gameObject;
        cProjectile.Activation = true;
        goProjectile.name = Names.NAME_BULLET;
        Instantiate( goProjectile, m_goGun.transform.position, Quaternion.identity );

        m_dictInventory[ strProjectileName ]--;
    }
}